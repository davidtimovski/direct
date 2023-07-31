using System;
using CommunityToolkit.Mvvm.Input;
using Direct.Desktop.Services;
using Direct.Desktop.Utilities;
using Direct.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Graphics;

namespace Direct.Desktop;

public sealed partial class MainWindow : Window
{
    private readonly ISettingsService _settingsService;
    private readonly IChatService _chatService;
    private readonly IServiceProvider _serviceProvider;

    private bool activated;
    private SettingsWindow? settingsWindow;
    private NewContactWindow? newContactWindow;
    private EditContactWindow? editContactWindow;

    public MainViewModel ViewModel { get; }

    public MainWindow(ISettingsService settingsService, IChatService chatService, IServiceProvider serviceProvider, MainViewModel viewModel)
    {
        InitializeComponent();

        Activated += WindowActivated;
        Closed += WindowClosed;

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        _settingsService = settingsService;
        _chatService = chatService;
        _serviceProvider = serviceProvider;

        ViewModel = viewModel;
    }

    // Hack, find a method that can execute asynchronously on first window creation
    private async void WindowActivated(object _, WindowActivatedEventArgs args)
    {
        if (activated)
        {
            return;
        }

        activated = true;

        var successful = await ViewModel.InitializeAsync();
        if (successful)
        {
            return;
        }

        var closeCommand = new StandardUICommand();
        closeCommand.ExecuteRequested += (XamlUICommand sender, ExecuteRequestedEventArgs args) =>
        {
            sender.DispatcherQueue.TryEnqueue(Close);
        };

        var dialog = new ContentDialog
        {
            XamlRoot = Content.XamlRoot,
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            RequestedTheme = _settingsService.Theme,
            Title = "Could not connect",
            Content = "Do you want me to keep retrying every minute?",
            PrimaryButtonText = "Yes, please",
            CloseButtonText = "No, exit",
            PrimaryButtonCommand = new RelayCommand(_chatService.StartConnectionRetry),
            CloseButtonCommand = closeCommand
        };
        await dialog.ShowAsync();
    }

    private async void WindowClosed(object _, WindowEventArgs args)
    {
        var windowSize = WindowingUtil.GetSize(this);

        _settingsService.WindowWidth = windowSize.Width;
        _settingsService.WindowHeight = windowSize.Height;
        _settingsService.Save();

        await _chatService.DisconnectAsync();
    }

    private void SettingsButton_Click(object _, RoutedEventArgs e)
    {
        if (settingsWindow is not null)
        {
            settingsWindow.Activate();
            return;
        }

        settingsWindow = _serviceProvider.GetRequiredService<SettingsWindow>();
        settingsWindow.Closed += (object _, WindowEventArgs args) =>
        {
            settingsWindow = null;
        };
        WindowingUtil.Resize(settingsWindow, new SizeInt32(400, 150));
        settingsWindow.Activate();
    }

    private void AddNewContactButton_Click(object _, RoutedEventArgs e)
    {
        if (newContactWindow is not null)
        {
            newContactWindow.Activate();
            return;
        }

        newContactWindow = _serviceProvider.GetRequiredService<NewContactWindow>();
        newContactWindow.Closed += (object _, WindowEventArgs args) =>
        {
            newContactWindow = null;
        };
        WindowingUtil.Resize(newContactWindow, new SizeInt32(370, 280));
        newContactWindow.Activate();
    }

    private void EditContact_Click(object _, RoutedEventArgs e)
    {
        if (editContactWindow is not null)
        {
            editContactWindow.Activate();
            return;
        }

        editContactWindow = new EditContactWindow(new EditContactViewModel(
                _serviceProvider.GetRequiredService<ISettingsService>(),
                _serviceProvider.GetRequiredService<IEventService>(),
                ViewModel.SelectedContact!.UserId.ToString("N"),
                ViewModel.SelectedContact!.Nickname
            ));
        editContactWindow.Closed += (object sender, WindowEventArgs args) =>
        {
            editContactWindow = null;
        };
        WindowingUtil.Resize(editContactWindow, new SizeInt32(370, 280));
        editContactWindow.Activate();
    }
}
