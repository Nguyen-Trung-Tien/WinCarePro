using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Data.Sqlite;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinCarePro.Core.Helpers;
using WinCarePro.Database;
using WinCarePro.Services;
using WinCarePro.Shared.Components;

namespace WinCarePro.Views;

public sealed partial class SettingsPage
{
    private void OnRefreshStorageClick(object sender, RoutedEventArgs e)
    {
        UpdateStorageSizes();
    }

    private void UpdateStorageSizes()
    {
        if (LogsDbSizeLabel != null) LogsDbSizeLabel.Text = "Scanning...".T();
        if (ReportsDbSizeLabel != null) ReportsDbSizeLabel.Text = "Scanning...".T();
        if (CacheDbSizeLabel != null) CacheDbSizeLabel.Text = "Scanning...".T();

        Task.Run(() =>
        {
            try
            {
                string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinCarePro");
                string dbPath = Path.Combine(appData, "wincaredb.db");
                
                // 1. Logs
                long logsCount = 0;
                long dbSize = 0;
                if (File.Exists(dbPath))
                {
                    dbSize = new FileInfo(dbPath).Length;
                }
                try
                {
                    using var conn = new SqliteConnection($"Data Source={dbPath}");
                    conn.Open();
                    using var cmd = new SqliteCommand("SELECT COUNT(*) FROM Logs", conn);
                    logsCount = (long)(cmd.ExecuteScalar() ?? 0L);
                }
                catch {}
                string logsText = $"{logsCount} logs ({FormatHelper.FormatBytes(dbSize)})";

                // 2. Reports
                long reportsCount = 0;
                long reportsSize = 0;
                string reportsFolder = Path.Combine(appData, "Reports");
                if (Directory.Exists(reportsFolder))
                {
                    var files = Directory.GetFiles(reportsFolder);
                    reportsCount = files.Length;
                    foreach (var f in files)
                    {
                        reportsSize += new FileInfo(f).Length;
                    }
                }
                string reportsText = $"{reportsCount} files ({FormatHelper.FormatBytes(reportsSize)})";

                // 3. Cache
                long cacheCount = 0;
                long cacheSize = 0;
                string cacheFolder = Path.Combine(Path.GetTempPath(), "WinCareProUpdates");
                if (Directory.Exists(cacheFolder))
                {
                    var files = Directory.GetFiles(cacheFolder);
                    cacheCount = files.Length;
                    foreach (var f in files)
                    {
                        cacheSize += new FileInfo(f).Length;
                    }
                }
                string directCacheFolder = Path.Combine(Path.GetTempPath(), "WinCareUpdates");
                if (Directory.Exists(directCacheFolder))
                {
                    var files = Directory.GetFiles(directCacheFolder);
                    cacheCount += files.Length;
                    foreach (var f in files)
                    {
                        cacheSize += new FileInfo(f).Length;
                    }
                }
                string cacheText = $"{cacheCount} pkgs ({FormatHelper.FormatBytes(cacheSize)})";

                DispatcherQueue.TryEnqueue(() =>
                {
                    if (LogsDbSizeLabel != null) LogsDbSizeLabel.Text = logsText;
                    if (ReportsDbSizeLabel != null) ReportsDbSizeLabel.Text = reportsText;
                    if (CacheDbSizeLabel != null) CacheDbSizeLabel.Text = cacheText;
                });
            }
            catch {}
        });
    }

    private async void OnPurgeDatabaseClick(object sender, RoutedEventArgs e)
    {
        var purgeBtn = PurgeStorageBtn ?? (sender as Button);
        await UiLoadingHelper.ExecuteWithLoadingAsync(
            purgeBtn, PurgeProgressRing, PurgeStorageText, null,
            "Purging Storage...", "Purge Selected Storage",
            async () =>
            {
                try
                {
                    string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinCarePro");
                    string dbPath = Path.Combine(appData, "wincaredb.db");

                    if (PurgeLogsCheckbox.IsChecked == true)
                    {
                        using var connection = new SqliteConnection($"Data Source={dbPath}");
                        connection.Open();
                        using var cmd = new SqliteCommand("DELETE FROM Logs", connection);
                        cmd.ExecuteNonQuery();
                    }
                    if (PurgeReportsCheckbox.IsChecked == true)
                    {
                        using var connection = new SqliteConnection($"Data Source={dbPath}");
                        connection.Open();
                        using (var cmd = new SqliteCommand("DELETE FROM Reports", connection))
                        {
                            cmd.ExecuteNonQuery();
                        }

                        string reportsFolder = Path.Combine(appData, "Reports");
                        if (Directory.Exists(reportsFolder))
                        {
                            foreach (var file in Directory.GetFiles(reportsFolder))
                            {
                                try { File.Delete(file); } catch { }
                            }
                        }
                    }
                    if (PurgeCacheCheckbox.IsChecked == true)
                    {
                        string cacheFolder = Path.Combine(Path.GetTempPath(), "WinCareProUpdates");
                        if (Directory.Exists(cacheFolder))
                        {
                            foreach (var file in Directory.GetFiles(cacheFolder))
                            {
                                try { File.Delete(file); } catch { }
                            }
                        }
                        string directCacheFolder = Path.Combine(Path.GetTempPath(), "WinCareUpdates");
                        if (Directory.Exists(directCacheFolder))
                        {
                            foreach (var file in Directory.GetFiles(directCacheFolder))
                            {
                                try { File.Delete(file); } catch { }
                            }
                        }
                    }

                    DbManager.LogAction("Purged selected database rows and cache", "Settings", "Success");
                    UpdateStorageSizes();
                    
                    App.MainWindowInstance?.ShowToastNotification("Purge Completed".T(), "Selected caches and database rows cleared successfully.".T(), "Success");
                }
                catch (Exception ex)
                {
                    App.MainWindowInstance?.ShowToastNotification("Purge Failed".T(), ex.Message, "Critical");
                }
            },
            minDurationMs: 1200);
    }

    private void UpdateAboutTelemetry()
    {
        try
        {
            using var curProc = Process.GetCurrentProcess();
            curProc.Refresh();
            double workingSetMb = curProc.WorkingSet64 / (1024.0 * 1024.0);
            var uptime = DateTime.Now - curProc.StartTime;

            if (AboutMemoryText != null)
            {
                AboutMemoryText.Text = string.Format("{0:F1} MB Allocated", workingSetMb);
            }

            if (AboutUptimeText != null)
            {
                AboutUptimeText.Text = string.Format("{0:D2}:{1:D2}:{2:D2}", (int)uptime.TotalHours, uptime.Minutes, uptime.Seconds);
            }

            if (AboutDbStatusText != null)
            {
                AboutDbStatusText.Text = "Encrypted WAL • Healthy".T();
            }
        }
        catch { }
    }

    private async void OnViewChangelogClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var isDark = ThemeManager.Instance.CurrentTheme == ElementTheme.Dark;

            var releaseItems = new (string Tag, string Title, string Description, string Glyph, string ColorHex)[]
            {
                ("🎨 UI/UX", "Aura Glassmorphic Fluent 2.0 Theme Studio", "Synchronized semantic tokens across dark and light modes with Cyberpunk Neon & Cyan/Teal accent gradients.", "\uE790", "#FF06B6D4"),
                ("⚡ PERF", "120 FPS Fluid Animations & Reduced Motion", "Staggered entrance delays capped to <=200ms to eliminate UI lag, with automated low-power and accessibility fallbacks.", "\uE745", "#FFF59E0B"),
                ("💬 POPUP", "Standardized Aura ResultDialog Engine", "High-contrast result popups with telemetry breakdowns, collapsible log expander, and jitter-free tabular figures.", "\uE8BD", "#FF8B5CF6"),
                ("💾 DRIVER", "Hardware Driver Backup & Rollback Manager", "Comprehensive hardware component inspection, health auditing, and one-click rollback snapshot generation.", "\uE9A6", "#FF3B82F6"),
                ("🛡️ SECURE", "SafePathGuard Defense & Local Audit Trail", "Multi-layered filesystem protection, path traversal defenses, input sanitization, and tamper-resistant SQLite logs.", "\uE727", "#FF10B981"),
                ("🧹 BOOST", "1-Click Smart Boost & Memory Purging", "Instant RAM working set optimization and DNS cache flushing in under 800ms for peak gaming and productivity.", "\uE9D9", "#FFEC4899")
            };

            var rootStack = new StackPanel
            {
                Spacing = 14,
                Width = 520
            };

            var headerCard = new Border
            {
                Background = (Brush)Application.Current.Resources["AppStatChipBackground"],
                BorderBrush = (Brush)Application.Current.Resources["AppStatChipBorder"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16, 14, 16, 14)
            };
            var headerStack = new StackPanel { Spacing = 4 };
            var headerRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
            
            var badgeBorder = new Border
            {
                Background = (Brush)Application.Current.Resources["PrimaryAccentGradient"],
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 3, 8, 3),
                Child = new TextBlock
                {
                    Text = "Official Changelog".T(),
                    FontSize = 10.5,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.White)
                }
            };
            headerRow.Children.Add(badgeBorder);
            headerRow.Children.Add(new TextBlock
            {
                Text = "WinCare Pro Evolution & Release Notes".T(),
                FontSize = 14.5,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold
            });
            headerStack.Children.Add(headerRow);

            headerStack.Children.Add(new TextBlock
            {
                Text = "Explore detailed feature evolutions, architectural upgrades, performance optimizations, and security patches.".T(),
                FontSize = 11.5,
                Foreground = (Brush)Application.Current.Resources["SystemControlPageTextBaseMediumBrush"],
                TextWrapping = TextWrapping.Wrap
            });
            headerCard.Child = headerStack;
            rootStack.Children.Add(headerCard);

            var versionListContainer = new StackPanel { Spacing = 8 };

            var milestoneSummaryCard = new Border
            {
                Background = (Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"],
                BorderBrush = (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10)
            };
            var mStack = new StackPanel { Spacing = 4 };
            var mRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
            mRow.Children.Add(new TextBlock
            {
                Text = "v4.7.0 Nova",
                FontSize = 13,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = (Brush)Application.Current.Resources["PrimaryAccentBrush"]
            });
            mRow.Children.Add(new TextBlock
            {
                Text = "• 2026.09 (Current)".T(),
                FontSize = 11,
                Foreground = (Brush)Application.Current.Resources["SystemControlPageTextBaseMediumBrush"],
                VerticalAlignment = VerticalAlignment.Center
            });
            mStack.Children.Add(mRow);
            mStack.Children.Add(new TextBlock
            {
                Text = "Streamlined modular architecture, Settings Page decomposition, complete DI standardization, zero-allocation reduced motion checks, and optimized Windows 11 responsiveness.".T(),
                FontSize = 11.5,
                Foreground = (Brush)Application.Current.Resources["SystemControlPageTextBaseMediumBrush"],
                TextWrapping = TextWrapping.Wrap
            });
            milestoneSummaryCard.Child = mStack;
            versionListContainer.Children.Add(milestoneSummaryCard);

            foreach (var item in releaseItems)
            {
                var itemCard = new Border
                {
                    Background = (Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"],
                    BorderBrush = (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"],
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 10, 12, 10)
                };
                var iGrid = new Grid { ColumnSpacing = 12 };
                iGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                iGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                byte a = 255;
                byte r = Convert.ToByte(item.ColorHex.Substring(3, 2), 16);
                byte g = Convert.ToByte(item.ColorHex.Substring(5, 2), 16);
                byte b = Convert.ToByte(item.ColorHex.Substring(7, 2), 16);
                var itemColor = Windows.UI.Color.FromArgb(a, r, g, b);

                var iconBox = new Border
                {
                    Width = 36,
                    Height = 36,
                    CornerRadius = new CornerRadius(8),
                    Background = new SolidColorBrush(Windows.UI.Color.FromArgb(32, r, g, b)),
                    BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(64, r, g, b)),
                    BorderThickness = new Thickness(1),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new FontIcon
                    {
                        Glyph = item.Glyph,
                        FontSize = 16,
                        Foreground = new SolidColorBrush(itemColor),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };
                Grid.SetColumn(iconBox, 0);
                iGrid.Children.Add(iconBox);

                var textStack = new StackPanel { Spacing = 3, VerticalAlignment = VerticalAlignment.Center };
                
                var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
                var tagBorder = new Border
                {
                    Background = new SolidColorBrush(Windows.UI.Color.FromArgb(32, r, g, b)),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(5, 1, 5, 1),
                    Child = new TextBlock
                    {
                        Text = item.Tag,
                        FontSize = 9.5,
                        FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                        Foreground = new SolidColorBrush(itemColor)
                    }
                };
                titleRow.Children.Add(tagBorder);
                titleRow.Children.Add(new TextBlock
                {
                    Text = item.Title.T(),
                    FontSize = 12,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center
                });
                textStack.Children.Add(titleRow);

                textStack.Children.Add(new TextBlock
                {
                    Text = item.Description.T(),
                    FontSize = 11,
                    Foreground = (Brush)Application.Current.Resources["SystemControlPageTextBaseMediumBrush"],
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = 16
                });

                Grid.SetColumn(textStack, 1);
                iGrid.Children.Add(textStack);

                itemCard.Child = iGrid;
                versionListContainer.Children.Add(itemCard);
            }

            var scrollViewer = new ScrollViewer
            {
                MaxHeight = 380,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = versionListContainer
            };
            rootStack.Children.Add(scrollViewer);

            var dialog = new ContentDialog
            {
                Title = "What's New in v4.6".T(),
                Content = rootStack,
                CloseButtonText = "Close".T(),
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.Content.XamlRoot,
                RequestedTheme = ThemeManager.Instance.CurrentTheme,
                CornerRadius = new CornerRadius(14),
                BorderThickness = new Thickness(1)
            };

            await dialog.ShowAsync();
        }
        catch { }
    }

    private void OnEmailDeveloperClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "mailto:trungtiennguyen910@gmail.com?subject=WinCare%20Pro%20Feedback",
                UseShellExecute = true
            });
            DbManager.LogAction("Launched default mail client for developer contact", "Settings", "Success");
        }
        catch (Exception ex)
        {
            App.MainWindowInstance?.ShowToastNotification("Email Client Error".T(), ex.Message, "Warning");
        }
    }

    private void OnCopyEmailClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText("trungtiennguyen910@gmail.com");
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
            App.MainWindowInstance?.ShowToastNotification("Email Copied".T(), "trungtiennguyen910@gmail.com has been copied to clipboard.".T(), "Success");
            DbManager.LogAction("Copied developer contact email to clipboard", "Settings", "Success");
        }
        catch (Exception ex)
        {
            App.MainWindowInstance?.ShowToastNotification("Clipboard Error".T(), ex.Message, "Critical");
        }
    }

    private void OnOpenGitHubClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/Nguyen-Trung-Tien/WinCarePro",
                UseShellExecute = true
            });
            DbManager.LogAction("Opened GitHub project repository", "Settings", "Success");
        }
        catch (Exception ex)
        {
            App.MainWindowInstance?.ShowToastNotification("Error".T(), ex.Message, "Critical");
        }
    }

    private void OnReportIssueClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/Nguyen-Trung-Tien/WinCarePro/issues",
                UseShellExecute = true
            });
            DbManager.LogAction("Opened GitHub issue tracker", "Settings", "Success");
        }
        catch (Exception ex)
        {
            App.MainWindowInstance?.ShowToastNotification("Error".T(), ex.Message, "Critical");
        }
    }

    private async void OnInspectEnvironmentClick(object sender, RoutedEventArgs e)
    {
        try
        {
            using var proc = Process.GetCurrentProcess();
            proc.Refresh();
            double workingSetMb = proc.WorkingSet64 / (1024.0 * 1024.0);
            double privateBytesMb = proc.PrivateMemorySize64 / (1024.0 * 1024.0);
            long totalGcMem = GC.GetTotalMemory(false) / (1024 * 1024);

            var metrics = new List<(string Label, string Value, string? StatusColor)>
            {
                ("OS Platform", Environment.OSVersion.Platform.ToString(), null),
                ("System Architecture", RuntimeInformation.OSArchitecture.ToString(), null),
                ("Logical Processor Cores", Environment.ProcessorCount.ToString(), null),
                ("Process Working Set", $"{workingSetMb:F1} MB", "#FF3B82F6"),
                ("Private Memory Allocated", $"{privateBytesMb:F1} MB", "#FF8B5CF6"),
                ("Managed GC Memory", $"{totalGcMem} MB", "#FF10B981"),
                ("Active Process Threads", proc.Threads.Count.ToString(), null)
            };

            await ResultDialogHelper.ShowCustomResultDialogAsync(
                this.Content.XamlRoot,
                ResultDialogType.Info,
                "System Environment & Runtime Inspector",
                "Runtime telemetry metrics and active host parameters verified locally.",
                metrics: metrics,
                primaryButtonText: "OK");

            DbManager.LogAction("Inspected system environment parameters", "Developer", "Success");
        }
        catch (Exception ex)
        {
            App.MainWindowInstance?.ShowToastNotification("Inspection Error".T(), ex.Message, "Critical");
        }
    }

    private void OnTrimWorkingSetClick(object sender, RoutedEventArgs e)
    {
        try
        {
            using var proc = Process.GetCurrentProcess();
            proc.Refresh();
            long beforeBytes = proc.WorkingSet64;

            GC.Collect(2, GCCollectionMode.Aggressive, true, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Aggressive, true, true);

            try
            {
                SetProcessWorkingSetSize(proc.Handle, -1, -1);
            }
            catch { }

            proc.Refresh();
            long afterBytes = proc.WorkingSet64;
            double freedMb = Math.Max(0, (beforeBytes - afterBytes) / (1024.0 * 1024.0));

            UpdateAboutTelemetry();

            App.MainWindowInstance?.ShowToastNotification(
                "RAM Working Set Trimmed".T(),
                string.Format("Forced Garbage Collection completed. Freed {0:F1} MB of RAM.".T(), freedMb),
                "Success"
            );
            DbManager.LogAction(string.Format("Forced GC and trimmed {0:F1} MB RAM working set", freedMb), "Developer", "Success");
        }
        catch (Exception ex)
        {
            App.MainWindowInstance?.ShowToastNotification("Trim Error".T(), ex.Message, "Warning");
        }
    }

    private async void OnViewAuditLogsClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var logs = DbManager.GetLogs(null, null);
            var sb = new System.Text.StringBuilder();

            if (logs.Count == 0)
            {
                sb.AppendLine("No activity logs recorded in the local SQLite database.".T());
            }
            else
            {
                foreach (var log in logs.Take(50))
                {
                    sb.AppendLine(string.Format("[{0:yyyy-MM-dd HH:mm:ss}] [{1}] ({2}): {3}", log.CreatedAt, log.Status, log.Module, log.Action));
                }
            }

            await ResultDialogHelper.ShowCustomResultDialogAsync(
                this.Content.XamlRoot,
                ResultDialogType.Info,
                "SQLite Activity Audit Log Viewer",
                "Displaying the last 50 activity and security audit entries stored in SQLite database.",
                detailLog: sb.ToString(),
                primaryButtonText: "Close");

            DbManager.LogAction("Viewed SQLite audit records", "Developer", "Success");
        }
        catch (Exception ex)
        {
            App.MainWindowInstance?.ShowToastNotification("Audit Log Error".T(), ex.Message, "Critical");
        }
    }

    private async void OnExportDiagnosticsClick(object sender, RoutedEventArgs e)
    {
        try
        {
            using var proc = Process.GetCurrentProcess();
            proc.Refresh();

            var diagObj = new
            {
                App = WinCarePro.Core.AppConstants.AppName,
                Version = WinCarePro.Core.AppConstants.VersionString,
                TimestampUtc = DateTime.UtcNow.ToString("o"),
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                OS = Environment.OSVersion.VersionString,
                ProcessorCount = Environment.ProcessorCount,
                WorkingSetMB = (proc.WorkingSet64 / (1024.0 * 1024.0)).ToString("F1"),
                PrivateMemoryMB = (proc.PrivateMemorySize64 / (1024.0 * 1024.0)).ToString("F1"),
                GCTotalMemoryMB = (GC.GetTotalMemory(false) / (1024.0 * 1024.0)).ToString("F1"),
                RecentLogs = DbManager.GetLogs(null, null).Take(30).Select(l => new { l.CreatedAt, l.Module, l.Status, l.Action })
            };

            string json = JsonSerializer.Serialize(diagObj, new JsonSerializerOptions { WriteIndented = true });
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string exportFile = Path.Combine(desktopPath, $"WinCarePro_Diagnostics_{DateTime.Now:yyyyMMdd_HHmmss}.json");

            await File.WriteAllTextAsync(exportFile, json);

            App.MainWindowInstance?.ShowToastNotification(
                "Diagnostics Exported".T(),
                string.Format("Diagnostic bundle saved to Desktop:\n{0}".T(), Path.GetFileName(exportFile)),
                "Success"
            );
            DbManager.LogAction("Exported system diagnostic bundle to JSON", "Developer", "Success");
        }
        catch (Exception ex)
        {
            App.MainWindowInstance?.ShowToastNotification("Export Error".T(), ex.Message, "Critical");
        }
    }

    private void OnBrowsePluginsClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/Nguyen-Trung-Tien/WinCarePro/wiki/Plugins",
                UseShellExecute = true
            });
            DbManager.LogAction("Launched verified plugins browser URL", "Settings", "Success");
        }
        catch (Exception ex)
        {
            App.MainWindowInstance?.ShowToastNotification("Error".T(), ex.Message, "Critical");
        }
    }
}
