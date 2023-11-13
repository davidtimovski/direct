using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Direct.Desktop.Pages;
using Direct.Desktop.Services;
using Direct.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Graphics;

namespace Direct.Desktop;

public sealed partial class MainWindow : Window
{
    private readonly ISettingsService _settingsService;
    private readonly IConnectionService _connectionService;
    private readonly IServiceProvider _serviceProvider;

    private ProfileWindow? profileWindow;
    private SettingsWindow? settingsWindow;
    private NewContactWindow? newContactWindow;
    private PullMessagesWindow? pullMessagesWindow;
    private EditContactWindow? editContactWindow;

    public MainViewModel ViewModel { get; }
    public ICommand AddEmojiCommand { get; }

    public MainWindow(ISettingsService settingsService, IConnectionService connectionService, IServiceProvider serviceProvider, MainViewModel viewModel)
    {
        AppWindow.Resize(new SizeInt32(settingsService.WindowWidth, settingsService.WindowHeight));
        InitializeComponent();
        Title = "Direct";

        Closed += WindowClosed;

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        _settingsService = settingsService;
        _connectionService = connectionService;
        _serviceProvider = serviceProvider;

        ViewModel = viewModel;
        AddEmojiCommand = new RelayCommand<string>(AddEmoji);

        _ = InitializeAsync();
    }

    private void AddEmoji(string? emoji)
    {
        if (emoji is null)
        {
            return;
        }

        ViewModel.AddEmoji(emoji);
    }

    private async Task InitializeAsync()
    {
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
            PrimaryButtonCommand = new RelayCommand(_connectionService.StartConnectionRetry),
            CloseButtonCommand = closeCommand
        };
        await dialog.ShowAsync();
    }

    private async void MessageTextBoxEnter_Pressed(KeyboardAccelerator _, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        await ViewModel.MessageBoxEnterPressedAsync();
    }

    private void MessageTextBoxUp_Pressed(KeyboardAccelerator _, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        ViewModel.MessageBoxUpPressed();
    }

    private void MessageTextBoxEscape_Pressed(KeyboardAccelerator _, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        ViewModel.MessageBoxEscapePressed();
    }

    private async void WindowClosed(object _, WindowEventArgs args)
    {
        _settingsService.WindowWidth = AppWindow.Size.Width;
        _settingsService.WindowHeight = AppWindow.Size.Height;
        _settingsService.Save();

        await _connectionService.DisconnectAsync();
    }

    private async void ContactsListView_SelectionChanged(object _, SelectionChangedEventArgs e)
    {
        await ViewModel.SelectedContactChangedAsync();
        MessageTextBox.Focus(FocusState.Programmatic);
    }

    private void ProfileButton_Click(object _, RoutedEventArgs e)
    {
        if (profileWindow is not null)
        {
            profileWindow.Activate();
            return;
        }

        profileWindow = _serviceProvider.GetRequiredService<ProfileWindow>();
        profileWindow.Closed += (object _, WindowEventArgs args) =>
        {
            profileWindow = null;
        };
        profileWindow.Activate();
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
        newContactWindow.Activate();
    }

    private void PullMessages_Click(object _, RoutedEventArgs e)
    {
        if (pullMessagesWindow is not null)
        {
            pullMessagesWindow.Activate();
            return;
        }

        pullMessagesWindow = new PullMessagesWindow(
            _serviceProvider.GetRequiredService<IPullProxy>(),
            new PullMessagesViewModel(
                _serviceProvider.GetRequiredService<ISettingsService>(),
                _serviceProvider.GetRequiredService<IPullProxy>(),
                _serviceProvider.GetRequiredService<DispatcherQueue>(),
                ViewModel.SelectedContact!.Id,
                ViewModel.SelectedContact!.Nickname
            )
        );
        pullMessagesWindow.Closed += (object sender, WindowEventArgs args) =>
        {
            pullMessagesWindow = null;
        };
        pullMessagesWindow.Activate();
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
            ViewModel.SelectedContact!.Id.ToString("N"),
            ViewModel.SelectedContact!.Nickname
        ));
        editContactWindow.Closed += (object sender, WindowEventArgs args) =>
        {
            editContactWindow = null;
        };
        editContactWindow.Activate();
    }

    private async void DeleteContact_Click(object _, RoutedEventArgs e)
    {
        var content = new DeleteContactDialog(ViewModel.SelectedContact!.Nickname);
        var dialog = new ContentDialog
        {
            XamlRoot = Content.XamlRoot,
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            RequestedTheme = _settingsService.Theme,
            Title = "Delete contact",
            Content = content,
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            PrimaryButtonCommand = new RelayCommand(() => ViewModel.DeleteContactAsync(content.DeleteMessages))
        };
        await dialog.ShowAsync();
    }
}
