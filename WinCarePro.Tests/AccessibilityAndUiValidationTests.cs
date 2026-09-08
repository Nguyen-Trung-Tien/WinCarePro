using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

        // Fallback to current working directory
        if (File.Exists("WinCarePro.csproj"))
        {
            return Path.GetFullPath(".");
        }

        throw new DirectoryNotFoundException("Could not locate WinCarePro repository root.");
    }

    [Fact]
    public void AllXaml_ShouldNotContainPointerOrTapEventsOnLayoutPanels()
    {
        var root = GetSolutionRoot();
        var xamlFiles = Directory.GetFiles(root, "*.xaml", SearchOption.AllDirectories)
            .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\") && !f.Contains("/bin/") && !f.Contains("/obj/"))
            .ToList();

        Assert.NotEmpty(xamlFiles);

        var illegalPattern = new Regex(@"<(Border|Grid|StackPanel|RelativePanel)\b[^>]*(PointerPressed|Tapped|DoubleTapped)=", RegexOptions.IgnoreCase);

        var violations = (from file in xamlFiles
                          let lines = File.ReadAllLines(file)
                          from i in Enumerable.Range(0, lines.Length)
                          where illegalPattern.IsMatch(lines[i])
                          select $"{Path.GetFileName(file)}:Line {i + 1}: {lines[i].Trim()}").ToList();

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} layout panels with mouse-only PointerPressed/Tapped/DoubleTapped handlers. " +
            $"These must be replaced with keyboard-accessible controls (e.g. Button, ToggleButton, ListViewItem):\n" +
            string.Join("\n", violations));
    }

    [Fact]
    public void AppXaml_MustContainHighContrastThemeDictionary()
    {
        var root = GetSolutionRoot();
        var appXamlPath = Path.Combine(root, "App.xaml");
        Assert.True(File.Exists(appXamlPath), "App.xaml must exist.");

        var content = File.ReadAllText(appXamlPath);

        Assert.Contains("<ResourceDictionary x:Key=\"HighContrast\">", content);
        Assert.Contains("SystemColorWindowBrush", content);
        Assert.Contains("SystemColorWindowTextColorBrush", content);
        Assert.Contains("SystemColorHighlightBrush", content);
        Assert.Contains("SystemColorHighlightTextColorBrush", content);
    }

    [Fact]
    public void AppXaml_ButtonStyles_MustDefineVisibleFocusVisuals()
    {
        var root = GetSolutionRoot();
        var appXamlPath = Path.Combine(root, "App.xaml");
        var content = File.ReadAllText(appXamlPath);

        Assert.Contains("UseSystemFocusVisuals", content);
        Assert.Contains("FocusVisualPrimaryThickness", content);
    }

    [Fact]
    public void CorePages_MustDefineLiveRegionsOnDynamicStatusAreas()
    {
        var root = GetSolutionRoot();
        var filesToCheck = new[]
        {
            Path.Combine(root, "MainWindow.xaml"),
            Path.Combine(root, "Modules", "Dashboard", "DashboardPage.xaml"),
            Path.Combine(root, "Modules", "AiAssistant", "AiWinCareEnginePage.xaml"),
            Path.Combine(root, "Modules", "JunkCleaner", "JunkPage.xaml"),
            Path.Combine(root, "Modules", "Repair", "RepairPage.xaml"),
            Path.Combine(root, "Modules", "Updates", "UpdaterPage.xaml"),
            Path.Combine(root, "Shared", "Components", "ToastNotification.xaml")
        };

        foreach (var file in filesToCheck)
        {
            Assert.True(File.Exists(file), $"Target file {file} must exist.");
            var content = File.ReadAllText(file);
            Assert.True(content.Contains("AutomationProperties.LiveSetting"),
                $"File {Path.GetFileName(file)} should declare at least one AutomationProperties.LiveSetting for screen reader announcements.");
        }
    }

    [Fact]
    public void IconButtons_InKeyControls_MustDefineAccessibleNames()
    {
        var root = GetSolutionRoot();
        var filesToCheck = new[]
        {
            Path.Combine(root, "MainWindow.xaml"),
            Path.Combine(root, "MainPage.xaml"),
            Path.Combine(root, "Modules", "Settings", "SettingsPage.xaml"),
            Path.Combine(root, "Modules", "DesktopWidget", "DesktopWidgetWindow.xaml"),
            Path.Combine(root, "Shared", "Components", "ToastNotification.xaml")
        };

        foreach (var file in filesToCheck)
        {
            Assert.True(File.Exists(file), $"Target file {file} must exist.");
            var content = File.ReadAllText(file);
            Assert.Contains("AutomationProperties.Name", content);
        }
    }

    [Fact]
    public void DestructiveOperations_MustRequireConfirmationsWithNonGenericLabels()
    {
        var root = GetSolutionRoot();
        var uninstallCode = File.ReadAllText(Path.Combine(root, "Modules", "Uninstall", "UninstallPage.xaml.cs"));
        var registryCode = File.ReadAllText(Path.Combine(root, "Modules", "Registry", "RegistryPage.xaml.cs"));
        var repairCode = File.ReadAllText(Path.Combine(root, "Modules", "Repair", "RepairPage.xaml.cs"));

        Assert.Contains("ShowConfirmAsync", uninstallCode);
        Assert.Contains("Uninstall Now", uninstallCode);
        Assert.Contains("Wipe Leftovers Now", uninstallCode);

        Assert.Contains("ShowConfirmAsync", registryCode);
        Assert.Contains("Repair Registry Now", registryCode);

        Assert.Contains("ShowConfirmAsync", repairCode);
        Assert.Contains("Start Component Cleanup", repairCode);
        Assert.Contains("Reset Components Now", repairCode);
    }
}
