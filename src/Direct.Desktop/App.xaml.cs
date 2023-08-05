using Direct.Desktop.Services;
using Direct.Desktop.Storage;
using Direct.Desktop.Utilities;
using Direct.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Windows.Graphics;
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

    private void ConfigureServices(ServiceCollection services)
    {
        services.AddSingleton<SetupWindow>();
        services.AddSingleton<SetupViewModel>();

        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainViewModel>();

        services.AddTransient<SettingsWindow>();
        services.AddTransient<SettingsViewModel>();

        services.AddTransient<NewContactWindow>();
        services.AddTransient<NewContactViewModel>();

        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IChatService, ChatService>();
        services.AddSingleton<IEventService, EventService>();

        services.AddSingleton(DispatcherQueue.GetForCurrentThread());
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        const string settingName = "DatabaseInitialized";
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        if (localSettings.Values[settingName] is null)
        {
            Repository.InitializeDatabaseAsync().Wait();
            localSettings.Values[settingName] = true;
        }

        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        if (settingsService.UserId.HasValue)
        {
            // Show main window
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            WindowingUtil.Resize(mainWindow, new SizeInt32(settingsService.WindowWidth, settingsService.WindowHeight));
            mainWindow.Activate();
        }
        else
        {
            // Show setup window
            var setupWindow = _serviceProvider.GetRequiredService<SetupWindow>();
            WindowingUtil.Resize(setupWindow, new SizeInt32(500, 400));
            setupWindow?.Activate();
        }      
    }
}
