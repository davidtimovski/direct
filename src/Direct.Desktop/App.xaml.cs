using Direct.Services;
using Direct.Utilities;
using Direct.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace Direct;

public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();
        ServiceCollection services = new();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private void ConfigureServices(ServiceCollection services)
    {
        services.AddSingleton<LobbyWindow>();
        services.AddSingleton<LobbyViewModel>();

        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainViewModel>();

        services.AddSingleton<IStorageService, StorageService>();
        services.AddSingleton<IChatService, ChatService>();

        services.AddSingleton(DispatcherQueue.GetForCurrentThread());
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
