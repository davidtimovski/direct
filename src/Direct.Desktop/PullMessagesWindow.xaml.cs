using Direct.Desktop.Services;
using Direct.Desktop.ViewModels;
using Microsoft.UI.Xaml;

namespace Direct.Desktop;

public sealed partial class PullMessagesWindow : Window
{
    private readonly IPullProxy _pullProxy;

    public PullMessagesViewModel ViewModel { get; }

    public PullMessagesWindow(IPullProxy pullProxy, PullMessagesViewModel viewModel)
    {
        InitializeComponent();
        Title = $"Pull messages from {viewModel.ContactNickname}";

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        _pullProxy = pullProxy;

        Closed += WindowClosed;

        ViewModel = viewModel;
    }

    private void WindowClosed(object sender, WindowEventArgs args)
    {
        _pullProxy.CancelMessagePull();
    }
}
