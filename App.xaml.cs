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
                $"AppDomain Unhandled Exception:\n{e.ExceptionObject}");
        };

        this.UnhandledException += (sender, e) =>
        {
            WriteCrashLog("crash_unhandled.txt",
                $"Unhandled Exception:\nMessage: {e.Message}\nException: {e.Exception}\nStackTrace: {e.Exception?.StackTrace}");
        };
    }

    public static IServiceProvider Services { get; private set; } = null!;

    private static void ConfigureServices()
    {
        var services = new ServiceCollection();

        // Register core engines wrapped in services
        services.AddSingleton<IJunkCleanerService, JunkCleanerService>();
        services.AddSingleton<INetworkService, NetworkService>();

        // Register ViewModels
        services.AddTransient<NetworkViewModel>();
        services.AddTransient<JunkViewModel>();
        services.AddTransient<UninstallViewModel>();
        services.AddTransient<RepairViewModel>();
        services.AddTransient<SystemOptimizerViewModel>();
        services.AddTransient<StartupViewModel>();
        services.AddTransient<ProcessViewModel>();
        services.AddTransient<DiskViewModel>();
        services.AddTransient<HardwareViewModel>();
        services.AddTransient<RegistryViewModel>();
        services.AddTransient<UpdaterViewModel>();
        services.AddTransient<DriverViewModel>();

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
            // Initialize DI Container
            ConfigureServices();

            // Initialize SQLite database
            Database.DbManager.InitializeDatabase();

            // Check if launched in background mode
            var commandLineArgs = Environment.GetCommandLineArgs();
            if (commandLineArgs.Any(arg => arg.Equals("/background", StringComparison.OrdinalIgnoreCase) || 
                                           arg.Equals("-background", StringComparison.OrdinalIgnoreCase)))
            {
                RunSilentCleanup();
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

    private static void RunSilentCleanup()
    {
        try
        {
            var cleaner = new Engines.JunkCleanerEngine();
            // Scan for all categories
            var categories = cleaner.ScanJunkAsync().GetAwaiter().GetResult();
            // Perform clean
            long cleanedBytes = cleaner.CleanJunkAsync(categories).GetAwaiter().GetResult();

            Database.DbManager.LogAction(
                $"Silent background clean completed. Freed {(cleanedBytes / 1024.0 / 1024.0):F2} MB.", 
                "Background Scheduler", 
                "Success"
            );
        }
        catch (Exception ex)
        {
            Database.DbManager.LogAction(
                $"Silent background clean failed: {ex.Message}", 
                "Background Scheduler", 
                "Failed"
            );
        }
        finally
        {
            Environment.Exit(0);
        }
    }

    private static void WriteCrashLog(string fileName, string content)
    {
        try
        {
            if (!System.IO.Directory.Exists(CrashLogDir))
            {
                System.IO.Directory.CreateDirectory(CrashLogDir);
            }
            System.IO.File.WriteAllText(
                System.IO.Path.Combine(CrashLogDir, fileName),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\n{content}");
        }
        catch { }
    }
}
