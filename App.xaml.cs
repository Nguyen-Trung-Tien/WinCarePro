using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using WinCarePro.Services.Contracts;
using WinCarePro.Services.Implementations;
using WinCarePro.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinCarePro;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private Window? _window;
    
    public static MainWindow? MainWindowInstance => (Application.Current as App)?._window as MainWindow;

    public static Microsoft.UI.Dispatching.DispatcherQueue? MainDispatcherQueue { get; private set; }

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern uint RegisterWindowMessage(string lpString);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    public const string SingleInstanceMutexName = @"Local\WinCarePro_SingleInstance_Mutex";
    public const string SingleInstanceMessageName = "WinCarePro_Activate_SingleInstance";
    private static System.Threading.Mutex? _singleInstanceMutex;

    public static void ReleaseSingleInstanceMutex()
    {
        try
        {
            _singleInstanceMutex?.ReleaseMutex();
            _singleInstanceMutex?.Dispose();
            _singleInstanceMutex = null;
        }
        catch { }
    }

    private static readonly string CrashLogDir = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WinCarePro", "Logs"
    );

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();

        // Catch low-level CLR exceptions (e.g. missing DLLs, type load failures)
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            WriteCrashLog("crash_appdomain.txt",
                $"AppDomain Unhandled Exception (IsTerminating: {e.IsTerminating}):\n{e.ExceptionObject}");
        };

        this.UnhandledException += (sender, e) =>
        {
            WriteCrashLog("crash_unhandled.txt",
                $"WinUI Unhandled Exception:\nMessage: {e.Message}\nException: {e.Exception}\nStackTrace: {e.Exception?.StackTrace}");
            
            // Do not suppress critical fatal system exceptions that corrupt state
            bool isFatal = e.Exception is OutOfMemoryException or StackOverflowException or AccessViolationException;
            if (!isFatal)
            {
                // Mark as handled to prevent application crash on recoverable UI errors
                e.Handled = true;
            }
        };

        // Catch unobserved task exceptions in asynchronous code
        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            WriteCrashLog("crash_unobserved.txt",
                $"Unobserved Task Exception:\nMessage: {e.Exception?.Message}\nException: {e.Exception}\nStackTrace: {e.Exception?.StackTrace}");
            e.SetObserved();
        };
    }

    public static IServiceProvider Services { get; private set; } = null!;

    private static void ConfigureServices()
    {
        var services = new ServiceCollection();

        // Register core engines wrapped in services
        services.AddSingleton<ISettingsService>(SettingsService.Instance);
        services.AddSingleton<IJunkCleanerService, JunkCleanerService>();
        services.AddSingleton<INetworkService, NetworkService>();
        services.AddSingleton<INetworkHistoryService, NetworkHistoryService>();
        services.AddSingleton<ILockingAppService, LockingAppService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<ISystemSnapshotService, SystemSnapshotService>();
        services.AddSingleton<IMaintenanceSchedulerService, MaintenanceSchedulerService>();
        services.AddSingleton<UndoManagerService>();
        services.AddSingleton<SmartFixService>();
        services.AddSingleton<TaskSchedulerService>(TaskSchedulerService.Instance);
        services.AddSingleton<IconCacheService>();
        services.AddSingleton<ServiceSafetyService>();
        services.AddSingleton<AuditLogService>();

        // Register engines in DI
        services.AddSingleton<Engines.AiDiagnosticsEngine>();
        services.AddSingleton<Engines.AiWinCareScoringEngine>();
        services.AddSingleton<Engines.PredictiveAnalysisEngine>();
        services.AddSingleton<Engines.JunkCleanerEngine>();
        services.AddSingleton<Engines.SecurityPrivacyEngine>();
        services.AddSingleton<Engines.SystemOptimizerEngine>();
        services.AddSingleton<Engines.StartupEngine>();
        services.AddSingleton<Engines.RegistryBackupEngine>();
        services.AddSingleton<Engines.SoftwareUpdaterEngine>();
        services.AddSingleton<Engines.HardwareDriverEngine>();
        services.AddSingleton<Engines.DiskEngine>();
        services.AddSingleton<Engines.ProcessService>();
        services.AddSingleton<Engines.NetworkEngine>();
        services.AddSingleton<Engines.ContextMenuEngine>();

        // Register ViewModels
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<NetworkViewModel>();
        services.AddTransient<JunkViewModel>();
        services.AddTransient<UninstallViewModel>();
        services.AddTransient<RepairViewModel>();
        services.AddTransient<SystemOptimizerViewModel>();
        services.AddTransient<StartupViewModel>();
        services.AddTransient<DiskViewModel>();
        services.AddTransient<RegistryViewModel>();
        services.AddTransient<UpdaterViewModel>();
        services.AddTransient<ContextMenuViewModel>();
        services.AddTransient<SecurityViewModel>();

        Services = services.BuildServiceProvider();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        try
        {
            var commandLineArgs = Environment.GetCommandLineArgs();
            bool isBackground = commandLineArgs.Any(arg => arg.Equals("/background", StringComparison.OrdinalIgnoreCase) || 
                                                           arg.Equals("-background", StringComparison.OrdinalIgnoreCase));

            // Single-instance enforcement: prevent duplicate parallel windows and tray icons
            if (!isBackground)
            {
                _singleInstanceMutex = new System.Threading.Mutex(true, SingleInstanceMutexName, out bool createdNew);
                if (!createdNew)
                {
                    // An instance is already running! Broadcast activation message to existing instance
                    uint msg = RegisterWindowMessage(SingleInstanceMessageName);
                    if (msg != 0)
                    {
                        const IntPtr HWND_BROADCAST = (IntPtr)0xffff;
                        PostMessage(HWND_BROADCAST, msg, IntPtr.Zero, IntPtr.Zero);
                    }

                    // Exit immediately without initializing UI or creating a second tray icon
                    Environment.Exit(0);
                    return;
                }
            }

            MainDispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

            // 1. Initialize SQLite Database synchronously on app entrypoint
            try
            {
                Database.DbManager.InitializeDatabase();
            }
            catch (Exception dbEx)
            {
                WriteCrashLog("crash_db_init.txt", $"Database initialization error: {dbEx}");
            }

            // 2. Load user settings into SettingsService before window or services creation
            try
            {
                SettingsService.Instance.LoadSettings();
                var startupSettings = SettingsService.Instance.CurrentSettings;
                var startupTheme = (startupSettings.Theme == "Light") ? ElementTheme.Light : ElementTheme.Dark;
                WinCarePro.Services.ThemeManager.Instance.ApplyTheme(startupTheme);
                WinCarePro.Services.ThemeManager.Instance.ApplyAccent(startupSettings.AccentColor ?? "Default");
                WinCarePro.Services.TranslationManager.Instance.CurrentLanguage = (startupSettings.LanguageIndex == 1) ? WinCarePro.Services.AppLanguage.Vietnamese : WinCarePro.Services.AppLanguage.English;
            }
            catch (Exception setEx)
            {
                WriteCrashLog("crash_settings_init.txt", $"Settings preload error: {setEx}");
            }

            // 3. Initialize DI Container
            ConfigureServices();

            // Check if launched in background mode
            if (isBackground)
            {
                Task.Run(async () =>
                {
                    await RunSilentCleanupAsync();
                    Environment.Exit(0);
                });
                return;
            }

            _window = new MainWindow();
            _window.Activate();
        }
        catch (Exception ex)
        {
            WriteCrashLog("crash_onlaunched.txt", ex.ToString());
            throw;
        }
    }

    private static async Task RunSilentCleanupAsync()
    {
        try
        {
            var cleaner = Services.GetRequiredService<Engines.JunkCleanerEngine>();
            // Scan for all categories
            var categories = await cleaner.ScanJunkAsync();
            
            // Calculate total size of junk
            long totalJunkBytes = 0;
            foreach (var category in categories)
            {
                totalJunkBytes += category.SizeBytes;
            }

            double triggerSizeGB = 5.0; // Default threshold
            try
            {
                string raw = Database.DbManager.GetSettings();
                if (!string.IsNullOrEmpty(raw))
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(raw);
                    if (doc.RootElement.TryGetProperty("AutoCleanupTriggerSizeGB", out var sizeProp))
                    {
                        triggerSizeGB = sizeProp.GetDouble();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Error reading AutoCleanupTriggerSizeGB: {ex.Message}");
            }

            double totalJunkGB = totalJunkBytes / 1024.0 / 1024.0 / 1024.0;
            if (totalJunkGB >= triggerSizeGB)
            {
                // Perform clean
                long cleanedBytes = await cleaner.CleanJunkAsync(categories);
                Database.DbManager.LogAction(
                    $"Silent background clean completed. Freed {(cleanedBytes / 1024.0 / 1024.0):F2} MB.", 
                    "Background Scheduler", 
                    "Success"
                );
            }
            else
            {
                Database.DbManager.LogAction(
                    $"Silent background clean skipped. Junk size ({totalJunkGB:F2} GB) is below trigger threshold ({triggerSizeGB:F1} GB).", 
                    "Background Scheduler", 
                    "Success"
                );
            }
        }
        catch (Exception ex)
        {
            Database.DbManager.LogAction(
                $"Silent background clean failed: {ex.Message}", 
                "Background Scheduler", 
                "Failed"
            );
        }
    }

    private static void WriteCrashLog(string fileName, string content)
    {
        try
        {
            Infrastructure.Logging.CrashLogger.LogMessage($"App.{fileName}", content);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[App] Failed to write crash log '{fileName}': {ex.Message}");
        }
    }

    public static void ApplyAccentColor(string tag)
    {
        WinCarePro.Services.ThemeManager.Instance.ApplyAccent(tag);
    }
}
