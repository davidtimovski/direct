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
        InitializeDatabase();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private void ConfigureServices(ServiceCollection services)
    {
        services.AddSingleton<LobbyWindow>();
        services.AddSingleton<LobbyViewModel>();

        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainViewModel>();

        services.AddTransient<NewContactWindow>();
        services.AddTransient<NewContactViewModel>();

        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IChatService, ChatService>();
        services.AddSingleton<IEventService, EventService>();

        services.AddSingleton(DispatcherQueue.GetForCurrentThread());
    }

    private static void InitializeDatabase()
    {
        const string settingName = "DatabaseInitialized";
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        if (localSettings.Values[settingName] is null)
        {
            Repository.InitializeDatabaseAsync();
            localSettings.Values[settingName] = true;
        }
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var lobbyWindow = _serviceProvider.GetService<LobbyWindow>()!;
        WindowingUtil.Resize(lobbyWindow, new SizeInt32(400, 300));
        lobbyWindow?.Activate();
    }
}
