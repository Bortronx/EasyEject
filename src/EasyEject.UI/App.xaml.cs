using EasyEject.Core.Contracts;
using EasyEject.Services;
using EasyEject.UI.Services;
using EasyEject.UI.ViewModels;
using EasyEject.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Serilog;

namespace EasyEject.UI;

/// <summary>
/// The WinUI 3 application.
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    /// <summary>
    /// Initializes the application and wires global exception logging.
    /// </summary>
    public App()
    {
        InitializeComponent();

        string logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EasyEject",
            "Logs");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Debug()
            .WriteTo.File(
                Path.Combine(logDirectory, "easyeject-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true)
            .CreateLogger();

        UnhandledException += OnUnhandledException;

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log.Error(args.Exception, "Unobserved task exception.");
            args.SetObserved();
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            Log.Fatal(args.ExceptionObject as Exception, "Unhandled app domain exception.");
        };
    }

    /// <inheritdoc />
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Log.Information("EasyEject starting (version {Version}).", AppInfo.Version);

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(ConfigureServices)
            .Build();
        Log.Information("Host built.");

        var window = _host.Services.GetRequiredService<MainWindow>();
        Log.Information("MainWindow resolved.");
        window.Closed += (_, _) => Shutdown();
        window.Activate();

        Log.Information("Main window activated.");
    }

    private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(Serilog.Log.Logger, dispose: false);
        });

        // Domain services
        services.AddSingleton<IDeviceEnumerator, WindowsDeviceEnumerator>();
        services.AddSingleton<IEjector, WindowsSafeEjectService>();
        services.AddSingleton<IDeviceManager, DeviceManager>();
        services.AddSingleton<IAppSettingsService, SettingsService>();

        // UI services
        services.AddSingleton<InfoBarNotificationService>();
        services.AddSingleton<INotificationService>(sp => sp.GetRequiredService<InfoBarNotificationService>());
        services.AddSingleton<DialogService>();
        services.AddSingleton<IUserConfirmationService>(sp => sp.GetRequiredService<DialogService>());
        services.AddSingleton<ThemeService>();
        services.AddSingleton<IThemeService>(sp => sp.GetRequiredService<ThemeService>());
        services.AddSingleton<WindowStateService>();

        // Views / view models
        services.AddSingleton<HomeViewModel>();
        services.AddSingleton<HomePage>();
        services.AddSingleton<MainWindow>();
    }

    /// <summary>
    /// Performs a clean shutdown of the application.
    /// </summary>
    public void Shutdown()
    {
        UnhandledException -= OnUnhandledException;
        _host?.Dispose();
        Log.CloseAndFlush();
        Exit();
    }

    /// <summary>
    /// Logs any unhandled exception before the process terminates.
    /// </summary>
    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unhandled exception");
        Log.Error("Exception HResult: 0x{0:X8}", unchecked((uint)e.Exception.HResult));
        Log.Error("Exception detail: {0}", e.Exception.ToString());
        for (Exception? inner = e.Exception.InnerException; inner != null; inner = inner.InnerException)
        {
            Log.Error("Inner exception ({0}): {1}", inner.GetType().FullName, inner);
        }

        _host?.Dispose();
        Log.CloseAndFlush();
    }
}
