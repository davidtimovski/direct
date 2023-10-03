using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Direct.Desktop.Services;
using Direct.Desktop.Storage;
using Direct.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Windows.Storage;

namespace Direct.Desktop;

public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;

    public App()
    {
        InitializeComponent();

        ServiceCollection services = new();

        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private static void ConfigureServices(ServiceCollection services)
    {
        services.AddSingleton<SetupWindow>();
        services.AddSingleton<SetupViewModel>();

        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainViewModel>();

        services.AddTransient<SettingsWindow>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<GeneralSettingsViewModel>();
        services.AddTransient<FeaturesSettingsViewModel>();

        services.AddTransient<NewContactWindow>();
        services.AddTransient<NewContactViewModel>();

        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IConnectionService, ConnectionService>();
        services.AddSingleton<IContactProxy, ContactProxy>();
        services.AddSingleton<IMessagingProxy, MessagingProxy>();
        services.AddSingleton<IPullProxy, PullProxy>();
        services.AddSingleton<IEventService, EventService>();

        services.AddSingleton(DispatcherQueue.GetForCurrentThread());
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        var isNewInstance = await CheckAppInstanceAsync();
        if (!isNewInstance)
        {
            // Exit our instance and stop
            Process.GetCurrentProcess().Kill();
            return;
        }

        const string settingName = "DatabaseInitialized";
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        if (localSettings.Values[settingName] is null)
        {
            await Repository.InitializeDatabaseAsync();
            localSettings.Values[settingName] = true;
        }

        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        if (settingsService.UserId.HasValue)
        {
            // Show main window
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Activate();
        }
        else
        {
            // Show setup window
            var setupWindow = _serviceProvider.GetRequiredService<SetupWindow>();
            setupWindow?.Activate();
        }
    }

    /// <summary>
    /// Helps make sure that we have only a single instance of the app running.
    /// </summary>
    private static async Task<bool> CheckAppInstanceAsync()
    {
        var appArgs = AppInstance.GetCurrent().GetActivatedEventArgs();

        // Get or register the main instance
        var mainInstance = AppInstance.FindOrRegisterForKey("main");

        // If the main instance isn't this current instance
        if (!mainInstance.IsCurrent)
        {
            // Redirect activation to that instance
            await mainInstance.RedirectActivationToAsync(appArgs);

            return false;
        }

        return true;
    }
}
