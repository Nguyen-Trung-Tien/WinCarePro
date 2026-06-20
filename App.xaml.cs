using Microsoft.UI.Xaml;

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

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        try
        {
            // Initialize SQLite database
            Database.DbManager.InitializeDatabase();

            _window = new MainWindow();
            _window.Activate();
        }
        catch (Exception ex)
        {
            WriteCrashLog("crash_onlaunched.txt", ex.ToString());
            throw;
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
