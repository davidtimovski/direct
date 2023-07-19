using System;
using Direct.Desktop.Services;
using Direct.Desktop.Utilities;
using Direct.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace Direct.Desktop;

public sealed partial class MainWindow : Window
{
    private readonly ISettingsService _settingsService;
    private readonly IChatService _chatService;
    private readonly IServiceProvider _serviceProvider;

    private NewContactWindow? newContactWindow;

    public MainViewModel ViewModel { get; }

    public MainWindow(ISettingsService settingsService, IChatService chatService, IServiceProvider serviceProvider, MainViewModel viewModel)
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        _settingsService = settingsService;
        _chatService = chatService;
        _serviceProvider = serviceProvider;

        ViewModel = viewModel;

        Closed += MainWindow_Closed;
    }

    private async void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        var windowSize = WindowingUtil.GetSize(this);

        _settingsService.WindowWidth = windowSize.Width;
        _settingsService.WindowHeight = windowSize.Height;
        _settingsService.Save();

        await _chatService.DisconnectAsync();
    }

    private void AddNewContactButton_Click(object sender, RoutedEventArgs e)
    {
        if (newContactWindow is not null)
        {
            newContactWindow.Activate();
            return;
        }

        newContactWindow = _serviceProvider.GetRequiredService<NewContactWindow>();
        newContactWindow.Closed += NewContactWindow_Closed;
        WindowingUtil.Resize(newContactWindow, new SizeInt32(400, 200));
        newContactWindow.Activate();
    }

    private void NewContactWindow_Closed(object sender, WindowEventArgs args)
    {
        newContactWindow = null;
    }
}
