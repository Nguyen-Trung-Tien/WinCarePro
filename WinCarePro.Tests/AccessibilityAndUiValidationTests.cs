using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using WinCarePro.Shared.Components;
using Xunit;

namespace WinCarePro.Tests;

public class AccessibilityAndUiValidationTests
{
    private static string GetSolutionRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            if (File.Exists(Path.Combine(current, "WinCarePro.csproj")))
            {
                return current;
            }
            var parent = Directory.GetParent(current);
            if (parent == null) break;
            current = parent.FullName;
        }

        if (File.Exists("WinCarePro.csproj"))
        {
            return Path.GetFullPath(".");
        }

        throw new DirectoryNotFoundException("Could not locate WinCarePro repository root.");
    }

    private static List<string> GetAllXamlFiles()
    {
        var root = GetSolutionRoot();
        return Directory.GetFiles(root, "*.xaml", SearchOption.AllDirectories)
            .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\") && !f.Contains("/bin/") && !f.Contains("/obj/"))
            .ToList();
    }

    #region 1. Keyboard Accessibility: Elimination of Mouse-Only Panel Handlers

    [Fact]
    public void AllXaml_ShouldNotContainPointerOrTapEventsOnLayoutPanels_WithPreciseLineInfo()
    {
        var xamlFiles = GetAllXamlFiles();
        Assert.NotEmpty(xamlFiles);

        var forbiddenLayoutTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Border", "Grid", "StackPanel", "RelativePanel", "Canvas", "Panel", "ContentControl"
        };

        var mouseOnlyEvents = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "PointerPressed", "PointerReleased", "Tapped", "DoubleTapped", "RightTapped"
        };

        var violations = new List<string>();

        foreach (var file in xamlFiles)
        {
            var fileName = Path.GetFileName(file);
            XDocument doc;
            try
            {
                doc = XDocument.Load(file, LoadOptions.SetLineInfo);
            }
            catch (Exception)
            {
                // Fallback to text analysis if XML parsing fails due to XAML specific entities
                var rawLines = File.ReadAllLines(file);
                for (int i = 0; i < rawLines.Length; i++)
                {
                    if (Regex.IsMatch(rawLines[i], @"<(Border|Grid|StackPanel|RelativePanel|Canvas)\b[^>]*(PointerPressed|Tapped|DoubleTapped)=", RegexOptions.IgnoreCase))
                    {
                        violations.Add($"{fileName}:Line {i + 1}: Mouse-only event found on layout panel: {rawLines[i].Trim()}");
                    }
                }
                continue;
            }

            foreach (var element in doc.Descendants())
            {
                if (!forbiddenLayoutTypes.Contains(element.Name.LocalName))
                    continue;

                foreach (var attr in element.Attributes())
                {
                    if (mouseOnlyEvents.Contains(attr.Name.LocalName))
                    {
                        var lineInfo = (IXmlLineInfo)attr;
                        int line = lineInfo.HasLineInfo() ? lineInfo.LineNumber : ((IXmlLineInfo)element).LineNumber;
                        var xName = element.Attributes().FirstOrDefault(a => a.Name.LocalName == "Name")?.Value ?? "Unnamed";

                        violations.Add($"{fileName}:Line {line}: <{element.Name.LocalName} x:Name=\"{xName}\"> defines mouse-only event '{attr.Name.LocalName}'. Replace with keyboard-accessible controls (Button, ToggleButton, or ListViewItem) to comply with WCAG 2.1.1 Keyboard.");
                    }
                }
            }
        }

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} layout panel(s) with mouse-only Pointer/Tap events:\n" +
            string.Join("\n", violations));
    }

    #endregion

    #region 2. Accessible Names: Enforcement on Icon-Only and Non-Text Buttons

    [Fact]
    public void AllXaml_ButtonsWithoutTextContent_MustDefineAutomationName()
    {
        var xamlFiles = GetAllXamlFiles();
        var violations = new List<string>();

        var buttonTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Button", "ToggleButton", "RepeatButton", "HyperlinkButton"
        };

        foreach (var file in xamlFiles)
        {
            var fileName = Path.GetFileName(file);
            XDocument doc;
            try
            {
                doc = XDocument.Load(file, LoadOptions.SetLineInfo);
            }
            catch
            {
                continue;
            }

            var buttons = doc.Descendants()
                .Where(e => buttonTypes.Contains(e.Name.LocalName))
                .Where(e => !e.Ancestors().Any(a => a.Name.LocalName is "ControlTemplate" or "Style"));

            foreach (var btn in buttons)
            {
                var lineInfo = (IXmlLineInfo)btn;
                int line = lineInfo.HasLineInfo() ? lineInfo.LineNumber : 0;
                var xName = btn.Attributes().FirstOrDefault(a => a.Name.LocalName == "Name")?.Value ?? "Unnamed";

                // Check 1: AutomationProperties.Name attribute
                var autoNameAttr = btn.Attributes().FirstOrDefault(a => a.Name.LocalName == "Name" && a.Name.NamespaceName.Contains("AutomationProperties")
                    || a.Name.LocalName == "AutomationProperties.Name");

                if (autoNameAttr != null && !string.IsNullOrWhiteSpace(autoNameAttr.Value))
                {
                    continue; // Has accessible name!
                }

                // Check 2: Content attribute with meaningful text
                var contentAttr = btn.Attributes().FirstOrDefault(a => a.Name.LocalName == "Content")?.Value;
                if (!string.IsNullOrWhiteSpace(contentAttr))
                {
                    // If content is data-bound or regular text (not a glyph symbol code)
                    if (contentAttr.StartsWith("{") || (!contentAttr.StartsWith("&#x") && contentAttr.Trim().Length > 1))
                    {
                        continue; // Has readable content
                    }
                }

                // Check 3: TextBlock child elements
                var textBlocks = btn.Descendants().Where(d => d.Name.LocalName == "TextBlock").ToList();
                bool hasText = textBlocks.Any(tb =>
                {
                    var textVal = tb.Attribute("Text")?.Value ?? tb.Value;
                    return !string.IsNullOrWhiteSpace(textVal) && !textVal.StartsWith("&#x");
                });

                if (hasText)
                {
                    continue; // Contains TextBlock with text
                }

                // If none of the above, this button has no readable text for screen readers!
                violations.Add($"{fileName}:Line {line}: <{btn.Name.LocalName} x:Name=\"{xName}\"> has icon/non-text content but lacks 'AutomationProperties.Name'. Screen readers (Narrator) cannot announce its purpose (WCAG 4.1.2).");
            }
        }

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} button(s) without accessible name or text content:\n" +
            string.Join("\n", violations));
    }

    #endregion

    #region 3. Live Regions: Targeted Catalog Verification for Dynamic Areas

    public record DynamicStatusArea(
        string RelativeFilePath,
        string ElementIdentifier,
        string ExpectedLiveSetting,
        string Description
    );

    [Fact]
    public void DynamicStatusAreas_MustDeclareRequiredLiveSetting_PoliteOrAssertive()
    {
        var root = GetSolutionRoot();

        var catalog = new List<DynamicStatusArea>
        {
            // Shell and System Telemetry
            new("MainWindow.xaml", "ClockText", "Polite", "System live clock text"),
            new("MainWindow.xaml", "NotificationBadge", "Polite", "Unread notification count badge"),
            new("MainWindow.xaml", "CpuChipText", "Polite", "Real-time CPU telemetry badge"),
            new("MainWindow.xaml", "RamChipText", "Polite", "Real-time RAM telemetry badge"),

            // Core Operation Progress & Statuses
            new(Path.Combine("Modules", "Dashboard", "DashboardPage.xaml"), "ViewModel.ScanStatus", "Polite", "System health scan progress status"),
            new(Path.Combine("Modules", "AiAssistant", "AiWinCareEnginePage.xaml"), "StatusTitleText", "Polite", "AI health status summary headline"),
            new(Path.Combine("Modules", "AiAssistant", "AiWinCareEnginePage.xaml"), "SummaryMessageText", "Polite", "AI predictive insights summary"),
            new(Path.Combine("Modules", "JunkCleaner", "JunkPage.xaml"), "ViewModel.ProgressMessage", "Polite", "Junk cleaner sweep operation progress"),
            new(Path.Combine("Modules", "Repair", "RepairPage.xaml"), "ViewModel.CurrentScanStepText", "Polite", "SFC/DISM repair operation progress"),
            new(Path.Combine("Modules", "Updates", "UpdaterPage.xaml"), "ViewModel.ActiveUpdatingAppName", "Polite", "Active software package update name"),
            new(Path.Combine("Modules", "Updates", "UpdaterPage.xaml"), "ViewModel.CurrentPhase", "Polite", "Software update download/install phase"),
            new(Path.Combine("Modules", "Notifications", "NotificationPage.xaml"), "DeckRecentPulseText", "Polite", "Recent notification activity pulse"),

            // Critical & Urgent Alerts
            new(Path.Combine("Shared", "Components", "ToastNotification.xaml"), "TitleTextBlock", "Assertive", "Toast notification alert headline"),
            new(Path.Combine("Shared", "Components", "ToastNotification.xaml"), "DescTextBlock", "Assertive", "Toast notification alert body message")
        };

        var errors = new List<string>();

        foreach (var target in catalog)
        {
            var filePath = Path.Combine(root, target.RelativeFilePath);
            Assert.True(File.Exists(filePath), $"Target XAML file '{target.RelativeFilePath}' must exist.");

            var content = File.ReadAllText(filePath);
            XDocument doc;
            try
            {
                doc = XDocument.Load(filePath, LoadOptions.SetLineInfo);
            }
            catch (Exception ex)
            {
                errors.Add($"{target.RelativeFilePath}: Failed to parse XML: {ex.Message}");
                continue;
            }

            // Find element by x:Name or by matching text/binding
            var matchedElement = doc.Descendants().FirstOrDefault(e =>
            {
                var nameAttr = e.Attributes().FirstOrDefault(a => a.Name.LocalName == "Name")?.Value;
                if (nameAttr == target.ElementIdentifier) return true;

                var textAttr = e.Attributes().FirstOrDefault(a => a.Name.LocalName == "Text")?.Value;
                if (textAttr != null && textAttr.Contains(target.ElementIdentifier, StringComparison.OrdinalIgnoreCase)) return true;

                return false;
            });

            if (matchedElement == null)
            {
                errors.Add($"{target.RelativeFilePath}: Could not find element matching identifier '{target.ElementIdentifier}' ({target.Description}).");
                continue;
            }

            var lineInfo = (IXmlLineInfo)matchedElement;
            int line = lineInfo.HasLineInfo() ? lineInfo.LineNumber : 0;

            var liveSettingAttr = matchedElement.Attributes().FirstOrDefault(a =>
                a.Name.LocalName == "LiveSetting" || a.Name.LocalName.EndsWith(".LiveSetting"));

            if (liveSettingAttr == null)
            {
                errors.Add($"{target.RelativeFilePath}:Line {line}: Element '{target.ElementIdentifier}' ({target.Description}) is missing 'AutomationProperties.LiveSetting=\"{target.ExpectedLiveSetting}\"'.");
            }
            else if (!liveSettingAttr.Value.Equals(target.ExpectedLiveSetting, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"{target.RelativeFilePath}:Line {line}: Element '{target.ElementIdentifier}' has LiveSetting=\"{liveSettingAttr.Value}\", expected \"{target.ExpectedLiveSetting}\" ({target.Description}).");
            }
        }

        Assert.True(errors.Count == 0,
            $"Found {errors.Count} dynamic status area live region defect(s):\n" +
            string.Join("\n", errors));
    }

    #endregion

    #region 4. Destructive Operation Standardization & Verification

    public record DestructiveWorkflow(
        string RelativeFilePath,
        string MethodName,
        string ExpectedActionVerb,
        string OperationInvocation
    );

    [Fact]
    public void DestructiveOperations_WorkflowsMustCallConfirmation_AndCancelSafely()
    {
        var root = GetSolutionRoot();

        var workflows = new List<DestructiveWorkflow>
        {
            new(Path.Combine("Modules", "Uninstall", "UninstallPage.xaml.cs"), "OnSingleUninstallClick", "Uninstall Now", "UninstallAppAsync"),
            new(Path.Combine("Modules", "Uninstall", "UninstallPage.xaml.cs"), "OnDeleteLeftoversClick", "Wipe Leftovers Now", "DeleteLeftoversAsync"),
            new(Path.Combine("Modules", "Uninstall", "UninstallPage.xaml.cs"), "OnDetailsUninstallClick", "Uninstall Now", "UninstallAppAsync"),
            new(Path.Combine("Modules", "Uninstall", "UninstallPage.xaml.cs"), "OnDetailsForceUninstallClick", "Force Remove Now", "UninstallSelectedAppsAsync"),
            new(Path.Combine("Modules", "Uninstall", "UninstallPage.xaml.cs"), "OnBatchUninstallClick", "Uninstall Selected", "UninstallSelectedAppsAsync"),
            new(Path.Combine("Modules", "Uninstall", "UninstallPage.xaml.cs"), "OnBatchForceUninstallClick", "Force Remove All Selected", "UninstallSelectedAppsAsync"),
            new(Path.Combine("Modules", "Registry", "RegistryPage.xaml.cs"), "OnRepairClick", "Repair Registry Now", "RepairSelectedAsync"),
            new(Path.Combine("Modules", "Repair", "RepairPage.xaml.cs"), "OnDismCleanClick", "Start Component Cleanup", "RunDismOperationAsync"),
            new(Path.Combine("Modules", "Repair", "RepairPage.xaml.cs"), "OnResetUpdateClick", "Reset Components Now", "RepairWindowsUpdateAsync"),
            new(Path.Combine("Modules", "Repair", "RepairPage.xaml.cs"), "OnRestoreServicesClick", "Restore Defaults Now", "RepairServicesConfigAsync"),
            new(Path.Combine("Modules", "Notifications", "NotificationPage.xaml.cs"), "OnClearNotificationsClick", "Purge All Notifications", "ClearAllNotifications"),
            new(Path.Combine("Modules", "Notifications", "NotificationPage.xaml.cs"), "OnClearOldLogsClick", "Purge Activity Logs", "CleanupOldLogs")
        };

        var genericWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OK", "Confirm", "Yes", "Apply", "Done" };
        var violations = new List<string>();

        foreach (var flow in workflows)
        {
            var filePath = Path.Combine(root, flow.RelativeFilePath);
            Assert.True(File.Exists(filePath), $"File '{flow.RelativeFilePath}' must exist.");

            var fileText = File.ReadAllText(filePath);

            // Locate the method in the source file
            int methodIndex = fileText.IndexOf(flow.MethodName, StringComparison.Ordinal);
            if (methodIndex < 0)
            {
                violations.Add($"{flow.RelativeFilePath}: Method '{flow.MethodName}' not found.");
                continue;
            }

            // Extract method block (from method definition to end of its block)
            int openBrace = fileText.IndexOf('{', methodIndex);
            if (openBrace < 0) continue;

            int braceCount = 1;
            int closeBrace = openBrace + 1;
            while (closeBrace < fileText.Length && braceCount > 0)
            {
                if (fileText[closeBrace] == '{') braceCount++;
                else if (fileText[closeBrace] == '}') braceCount--;
                closeBrace++;
            }

            var methodBody = fileText.Substring(openBrace, closeBrace - openBrace);

            // 1. Must invoke ShowConfirmAsync
            if (!methodBody.Contains("ResultDialogHelper.ShowConfirmAsync"))
            {
                violations.Add($"{flow.RelativeFilePath}: Method '{flow.MethodName}' does NOT invoke ResultDialogHelper.ShowConfirmAsync.");
                continue;
            }

            // 2. Dialog call must precede the destructive operation invocation
            int dialogIndex = methodBody.IndexOf("ResultDialogHelper.ShowConfirmAsync", StringComparison.Ordinal);
            int opIndex = methodBody.IndexOf(flow.OperationInvocation, StringComparison.Ordinal);

            if (opIndex >= 0 && dialogIndex > opIndex)
            {
                violations.Add($"{flow.RelativeFilePath}: Method '{flow.MethodName}' invokes destructive operation '{flow.OperationInvocation}' BEFORE confirmation dialog.");
            }

            // 3. Must check confirmation result to prevent execution on Cancel
            bool hasGuard = methodBody.Contains("if (confirmed)") ||
                            methodBody.Contains("if (!confirmed) return;") ||
                            methodBody.Contains("if (!confirmed)\n") ||
                            methodBody.Contains("if (!confirmed)\r\n");

            if (!hasGuard)
            {
                violations.Add($"{flow.RelativeFilePath}: Method '{flow.MethodName}' does NOT guard destructive operation with confirmation check (cancellation could execute operation).");
            }

            // 4. Must specify exact non-generic primary action label
            if (genericWords.Contains(flow.ExpectedActionVerb))
            {
                violations.Add($"Test configuration error: Action verb '{flow.ExpectedActionVerb}' is too generic.");
            }
            if (!methodBody.Contains(flow.ExpectedActionVerb))
            {
                violations.Add($"{flow.RelativeFilePath}: Method '{flow.MethodName}' does not use specific action verb '{flow.ExpectedActionVerb}'.");
            }
        }

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} destructive confirmation workflow violation(s):\n" +
            string.Join("\n", violations));
    }

    [Fact]
    public async Task ResultDialogHelper_ShowConfirmAsync_WhenCancelledOrNullRoot_ReturnsFalse()
    {
        // Behavioral test: when XamlRoot is null (headless environment or dialog dismissed),
        // ResultDialogHelper safely aborts and returns false without executing side effects.
        bool result = await ResultDialogHelper.ShowConfirmAsync(
            null!,
            "Test Destructive Action",
            "This operation requires explicit consent.",
            "Wipe Everything Now",
            "Cancel");

        Assert.False(result);
    }

    #endregion

    #region 5. High Contrast Theme Dictionary & Visible Focus Visuals

    [Fact]
    public void AppXaml_HighContrastDictionary_MustMapCoreSemanticTokensToSystemBrushes()
    {
        var root = GetSolutionRoot();
        var appXamlPath = Path.Combine(root, "App.xaml");
        Assert.True(File.Exists(appXamlPath), "App.xaml must exist.");

        XDocument doc = XDocument.Load(appXamlPath, LoadOptions.SetLineInfo);

        // Find HighContrast ResourceDictionary
        var highContrastDict = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "ResourceDictionary" &&
                                 e.Attributes().Any(a => a.Name.LocalName == "Key" && a.Value == "HighContrast"));

        Assert.NotNull(highContrastDict);

        var requiredSemanticTokens = new[]
        {
            "AppCardBackground",
            "AppCardBorder",
            "AppSecondaryCardBackground",
            "AppStatChipBackground",
            "AppBadgeGoodBg",
            "AppBadgeGoodBorder",
            "AppBadgeGoodFg",
            "AppBadgeCriticalBg",
            "AppBadgeCriticalBorder",
            "AppBadgeCriticalFg",
            "AppStatusGoodBrush",
            "AppStatusWarningBrush",
            "AppStatusDangerBrush",
            "AppStatusInfoBrush",
            "CpuAccentBrush",
            "RamAccentBrush",
            "TextFillColorPrimaryBrush",
            "TextFillColorSecondaryBrush",
            "ControlStrokeColorDefaultBrush",
            "CardStrokeColorDefaultBrush"
        };

        var mappedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var invalidStaticResources = new List<string>();
        var hardcodedHexColors = new List<string>();

        foreach (var child in highContrastDict.Elements())
        {
            var keyAttr = child.Attributes().FirstOrDefault(a => a.Name.LocalName == "Key")?.Value;
            if (!string.IsNullOrEmpty(keyAttr))
            {
                mappedKeys.Add(keyAttr);
            }

            // In High Contrast, tokens should be mapped to SystemColor* static resources
            if (child.Name.LocalName == "StaticResource")
            {
                var targetResource = child.Attributes().FirstOrDefault(a => a.Name.LocalName == "ResourceKey")?.Value;
                if (targetResource == null || !targetResource.StartsWith("SystemColor", StringComparison.OrdinalIgnoreCase))
                {
                    invalidStaticResources.Add($"Resource '{keyAttr}' maps to '{targetResource}', expected a SystemColor* contrast brush.");
                }
            }
            else if (child.Name.LocalName == "SolidColorBrush")
            {
                var colorAttr = child.Attributes().FirstOrDefault(a => a.Name.LocalName == "Color")?.Value;
                if (colorAttr != null && colorAttr.StartsWith("#"))
                {
                    hardcodedHexColors.Add($"Resource '{keyAttr}' defines hardcoded hex color '{colorAttr}' inside HighContrast dictionary, which violates system contrast themes.");
                }
            }
        }

        var missingTokens = requiredSemanticTokens.Where(t => !mappedKeys.Contains(t)).ToList();

        Assert.True(missingTokens.Count == 0,
            $"HighContrast ResourceDictionary is missing {missingTokens.Count} core semantic token(s):\n" +
            string.Join(", ", missingTokens));

        Assert.True(invalidStaticResources.Count == 0,
            $"HighContrast dictionary defines non-system resource mappings:\n" +
            string.Join("\n", invalidStaticResources));

        Assert.True(hardcodedHexColors.Count == 0,
            $"HighContrast dictionary defines hardcoded hex colors:\n" +
            string.Join("\n", hardcodedHexColors));
    }

    [Fact]
    public void AppXaml_ButtonStyles_MustDefineVisibleFocusVisuals()
    {
        var root = GetSolutionRoot();
        var appXamlPath = Path.Combine(root, "App.xaml");
        XDocument doc = XDocument.Load(appXamlPath, LoadOptions.SetLineInfo);

        var stylesToCheck = new[]
        {
            "DateTimeFlyoutCalendarButtonStyle",
            "VibrantPrimaryButtonStyle",
            "StandardSecondaryButtonStyle",
            "DangerButtonStyle"
        };

        var errors = new List<string>();

        foreach (var styleKey in stylesToCheck)
        {
            var styleElement = doc.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "Style" &&
                                     e.Attributes().Any(a => a.Name.LocalName == "Key" && a.Value == styleKey));

            if (styleElement == null)
            {
                errors.Add($"App.xaml is missing button style '{styleKey}'.");
                continue;
            }

            // Check for UseSystemFocusVisuals = True
            var focusVisualSetter = styleElement.Elements()
                .FirstOrDefault(s => s.Name.LocalName == "Setter" &&
                                     s.Attribute("Property")?.Value == "UseSystemFocusVisuals" &&
                                     s.Attribute("Value")?.Value.Equals("True", StringComparison.OrdinalIgnoreCase) == true);

            if (focusVisualSetter == null)
            {
                errors.Add($"Style '{styleKey}' does not specify <Setter Property=\"UseSystemFocusVisuals\" Value=\"True\" /> for visible keyboard focus outline.");
            }

            // Check for FocusVisualPrimaryThickness >= 2
            var thicknessSetter = styleElement.Elements()
                .FirstOrDefault(s => s.Name.LocalName == "Setter" &&
                                     s.Attribute("Property")?.Value == "FocusVisualPrimaryThickness");

            if (thicknessSetter == null)
            {
                errors.Add($"Style '{styleKey}' does not specify <Setter Property=\"FocusVisualPrimaryThickness\" Value=\"2\" />.");
            }
            else
            {
                if (double.TryParse(thicknessSetter.Attribute("Value")?.Value, out double val) && val < 2)
                {
                    errors.Add($"Style '{styleKey}' specifies focus visual thickness {val} < 2px minimum.");
                }
            }
        }

        Assert.True(errors.Count == 0,
            $"Found {errors.Count} button focus visual defect(s):\n" +
            string.Join("\n", errors));
    }

    #endregion
}
