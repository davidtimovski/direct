using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Direct.Desktop.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace Direct.Desktop.ViewModels;

public partial class PullMessagesViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IPullProxy _pullProxy;
    private readonly DispatcherQueue _dispatcherQueue;

    public PullMessagesViewModel(
        ISettingsService settingsService,
        IPullProxy pullProxy,
        DispatcherQueue dispatcherQueue,
        Guid contactId,
        string contactNickname)
    {
        _settingsService = settingsService;
        _settingsService.Changed += SettingsChanged;

        _pullProxy = pullProxy;
        _pullProxy.BatchReceived += MessagePullBatchReceived;
        _pullProxy.Completed += MessagePullCompleted;

        _dispatcherQueue = dispatcherQueue;

        Theme = _settingsService.Theme;
        ContactId = contactId;
        ContactNickname = contactNickname;
    }

    private void MessagePullBatchReceived(object? _, MessagePullBatchReceivedEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            MessagesPulled = e.Count;
        });
    }

    private void MessagePullCompleted(object? _, MessagePullCompletedEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            MessagesPulled = e.Pulled;

            if (e.Pulled == 0)
            {
                ResultText = "The contact did not have any messages with you.";
            }
            else if (e.Created == 0)
            {
                ResultText = $"None of the messages that were pulled were missing on our end.";
            }
            else if (e.Pulled == e.Created)
            {
                ResultText = "All of the messages that were pulled were missing on our end and were created.";
            }
            else
            {
                ResultText = $"{e.Created} of the messages that were pulled were missing on our end and were created.";
            }

            LoaderIsPaused = true;
            ResultTextIsVisible = true;
        });
    }

    public Guid ContactId { get; }
    public string ContactNickname { get; }

    private void SettingsChanged(object? _, SettingsChangedEventArgs e)
    {
        Theme = e.Theme;
    }

    [ObservableProperty]
    private ElementTheme theme;

    [ObservableProperty]
    private int messagesPulled;

    [ObservableProperty]
    private bool resultTextIsVisible;

    [ObservableProperty]
    private string resultText = string.Empty;

    [ObservableProperty]
    private bool startButtonIsVisible = true;

    [ObservableProperty]
    private bool loaderIsPaused = true;

    public async Task StartAsync()
    {
        LoaderIsPaused = false;
        StartButtonIsVisible = false;

        await _pullProxy.RequestMessagePullAsync(ContactId);
    }
}
