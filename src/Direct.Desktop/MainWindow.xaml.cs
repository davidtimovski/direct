using Chat.Utilities;
using Direct.Services;
using Direct.ViewModels;
using Microsoft.UI.Xaml;

namespace Direct;

public sealed partial class MainWindow : Window
{
    private readonly IStorageService _storageService;
    private readonly IChatService _chatService;

    public MainViewModel ViewModel { get; }

    public MainWindow(IStorageService storageService, IChatService chatService, MainViewModel viewModel)
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        _storageService = storageService;
        _chatService = chatService;

        ViewModel = viewModel;

        Closed += MainWindow_Closed;
    }

    private async void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        var windowSize = WindowingUtil.GetSize(this);

        _storageService.AppData.WindowWidth = windowSize.Width;
        _storageService.AppData.WindowHeight = windowSize.Height;
        _storageService.Store();

        await _chatService.DisconnectAsync();
    }
}
