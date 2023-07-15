using System.Threading.Tasks;
using Direct.Services;
using Direct.Utilities;
using Direct.ViewModels;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace Direct;

public sealed partial class LobbyWindow : Window
{
    private readonly IStorageService _storageService;
    private readonly MainWindow _mainWindow;

    public LobbyViewModel ViewModel { get; }

    public LobbyWindow(IStorageService storageService, MainWindow mainWindow, LobbyViewModel viewModel)
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        _storageService = storageService;
        _mainWindow = mainWindow;
        ViewModel = viewModel;
    }

    public async Task ConnectAsync()
    {
        await ViewModel.ConnectAsync();

        Close();

        WindowingUtil.Resize(_mainWindow, new SizeInt32(_storageService.AppData.WindowWidth, _storageService.AppData.WindowHeight));
        _mainWindow.Activate();
    }
}
