using Direct.Desktop.Services;
using Direct.Desktop.ViewModels;
using Microsoft.UI.Xaml;

namespace Direct.Desktop;

public sealed partial class PullMessagesWindow : Window
{
    private readonly IChatService _chatService;

    public PullMessagesViewModel ViewModel { get; }

    public PullMessagesWindow(IChatService chatService, PullMessagesViewModel viewModel)
    {
        InitializeComponent();
        Title = $"Pull messages from {viewModel.ContactNickname}";

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        _chatService = chatService;

        Closed += WindowClosed;

        ViewModel = viewModel;
    }

    private void WindowClosed(object sender, WindowEventArgs args)
    {
        _chatService.CancelMessagePull();
    }
}
