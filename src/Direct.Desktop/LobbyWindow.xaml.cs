using System;
using System.Threading.Tasks;
using Direct.Services;
using Direct.Utilities;
using Direct.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace Direct;

public sealed partial class LobbyWindow : Window
{
    private readonly IStorageService _storageService;
    private readonly IServiceProvider _serviceProvider;

    public LobbyViewModel ViewModel { get; }

    public LobbyWindow(IStorageService storageService, IServiceProvider serviceProvider, LobbyViewModel viewModel)
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        _storageService = storageService;
        _serviceProvider = serviceProvider;
        ViewModel = viewModel;
    }

    public async Task ConnectAsync()
    {
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();

        await ViewModel.ConnectAsync();

        Close();

        WindowingUtil.Resize(mainWindow, new SizeInt32(_storageService.AppData.WindowWidth, _storageService.AppData.WindowHeight));
        mainWindow.Activate();
    }
}
