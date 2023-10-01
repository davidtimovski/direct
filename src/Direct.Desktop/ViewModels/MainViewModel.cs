using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Direct.Desktop.Services;
using Direct.Desktop.Storage;
using Direct.Desktop.Storage.Entities;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace Direct.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IConnectionService _connectionService;
    private readonly IContactProxy _contactProxy;
    private readonly IMessagingProxy _messagingProxy;
    private readonly IEventService _eventService;
    private readonly DispatcherQueue _dispatcherQueue;

    public MainViewModel(
        ISettingsService settingsService,
        IConnectionService connectionService,
        IContactProxy contactProxy,
        IMessagingProxy messagingProxy,
        IPullProxy pullProxy,
        IEventService eventService,
        DispatcherQueue dispatcherQueue)
    {
        _settingsService = settingsService;
        _settingsService.Changed += SettingsChanged;

        _connectionService = connectionService;
        _connectionService.ConnectedContactsRetrieved += ConnectedContactsRetrieved;
        _connectionService.Reconnecting += Reconnecting;

        _contactProxy = contactProxy;
        _contactProxy.Connected += ContactConnected;
        _contactProxy.Disconnected += ContactDisconnected;
        _contactProxy.Added += ContactAdded;
        _contactProxy.Removed += ContactRemoved;

        _messagingProxy = messagingProxy;
        _messagingProxy.Sent += MessageSent;
        _messagingProxy.SendingFailed += MessageSendingFailed;
        _messagingProxy.Updated += MessageUpdated;
        _messagingProxy.UpdatingFailed += MessageUpdatingFailed;

        pullProxy.UpstreamStarted += MessagePullUpstreamStarted;
        pullProxy.UpstreamCompleted += MessagePullUpstreamCompleted;
        pullProxy.Completed += MessagePullCompleted;

        _eventService = eventService;
        _eventService.ContactAddedLocally += ContactAddedLocally;
        _eventService.ContactEditedLocally += ContactEdited;

        _dispatcherQueue = dispatcherQueue;

        Theme = _settingsService.Theme;
        EmojiPickerVisible = _settingsService.EmojiPickerEnabled;
        SpellCheckEnabled = _settingsService.SpellCheckEnabled;
        UserId = _settingsService.UserId!.Value.ToString("N");

        ConnectionStatus = new(_connectionService, dispatcherQueue);
    }

    [ObservableProperty]
    private ElementTheme theme;

    [ObservableProperty]
    private bool emojiPickerVisible;

    [ObservableProperty]
    private bool spellCheckEnabled;

    [ObservableProperty]
    private string userId = string.Empty;

    public ObservableCollection<ContactViewModel> Contacts = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ContactIsSelected))]
    private ContactViewModel? selectedContact;

    public bool ContactIsSelected => SelectedContact is not null;

    public ConnectionStatusViewModel ConnectionStatus { get; }

    public async Task<bool> InitializeAsync()
    {
        var contacts = await Repository.GetAllContactsAsync();

        foreach (var contact in contacts)
        {
            Contacts.Add(new ContactViewModel(contact.Id, contact.Nickname, _settingsService.Theme, _settingsService.MessageFontSize));
        }

        var contactIds = contacts.Select(x => x.Id).ToHashSet();
        return await _connectionService.ConnectAsync(_settingsService.UserId!.Value, contactIds);
    }

    public async Task SelectedContactChangedAsync()
    {
        if (SelectedContact is null)
        {
            return;
        }

        var now = DateTime.Now;

        if (SelectedContact.LastViewed is null || SelectedContact.HasUnreadMessages)
        {
            var recentMessages = await Repository.GetRecentMessagesAsync(SelectedContact.Id, SelectedContact.LastViewed);
            if (recentMessages.Count > 0)
            {
                SelectedContact.SetMessages(recentMessages);
            }

            if (SelectedContact.HasUnreadMessages)
            {
                SelectedContact.HasUnreadMessages = false;
            }
        }

        SelectedContact.LastViewed = now;
    }

    public async Task MessageBoxEnterPressedAsync()
    {
        var trimmedMessage = SelectedContact!.MessageText.Trim();
        if (trimmedMessage.Length == 0)
        {
            return;
        }

        SelectedContact!.MessageText = string.Empty;

        if (SelectedContact!.EditingMessageId.HasValue)
        {
            await _messagingProxy.UpdateMessageAsync(SelectedContact!.EditingMessageId.Value, SelectedContact!.Id, trimmedMessage);
            return;
        }

        await _messagingProxy.SendMessageAsync(SelectedContact!.Id, trimmedMessage);
    }

    public void MessageBoxUpPressed()
    {
        if (!SelectedContact!.EditingMessageId.HasValue && SelectedContact!.MessageText == string.Empty)
        {
            SelectLastSentMessageForUpdate();
        }
    }

    public void MessageBoxEscapePressed()
    {
        if (SelectedContact!.EditingMessageId.HasValue)
        {
            DeselectLastSentMessage();
        }
    }

    public void AddEmoji(string emoji)
    {
        SelectedContact!.MessageText += emoji;
    }

    public void CopyID()
    {
        var package = new DataPackage();
        package.SetText(UserId);
        Clipboard.SetContent(package);
    }

    public async void DeleteContactAsync(bool deleteMessages)
    {
        await Repository.DeleteContactAsync(SelectedContact!.Id, deleteMessages);
        await _contactProxy.RemoveContactAsync(SelectedContact!.Id);
    }

    private void SelectLastSentMessageForUpdate()
    {
        var lastSentMessage = SelectedContact!.MessageGroups.SelectMany(x => x).Where(x => !x.IsRecipient).OrderByDescending(x => x.SentAt).FirstOrDefault();
        if (lastSentMessage is null)
        {
            return;
        }

        SelectedContact!.EditingMessageId = lastSentMessage.Id;
        SelectedContact!.MessageText = lastSentMessage.Text;
        SelectedContact!.MessageSelectionStart = lastSentMessage.Text.Length;
        lastSentMessage.SetEditing();
    }

    private void DeselectLastSentMessage()
    {
        var message = SelectedContact!.MessageGroups.SelectMany(x => x).First(x => x.Id == SelectedContact!.EditingMessageId);

        SelectedContact!.EditingMessageId = null;
        SelectedContact!.MessageText = string.Empty;

        message?.SetTheme(_settingsService.Theme);
    }

    private void SettingsChanged(object? _, SettingsChangedEventArgs e)
    {
        foreach (var contact in Contacts)
        {
            contact.SetThemeAndFontSize(e.Theme, e.MessageFontSize);
        }

        Theme = e.Theme;
        SpellCheckEnabled = e.SpellCheckEnabled;
        EmojiPickerVisible = e.EmojiPickerEnabled;
    }

    private void ConnectedContactsRetrieved(object? eee, ConnectedContactsRetrievedEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            var connectedContacts = Contacts.Where(x => e.ConnectedUserIds.Contains(x.Id)).ToList();
            foreach (var contact in connectedContacts)
            {
                contact.Connected = true;
            }
        });
    }

    private void Reconnecting(object? sender, EventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            foreach (var contact in Contacts)
            {
                contact.Connected = false;
            }
        });
    }

    private void ContactConnected(object? _, ContactConnectedEventArgs e)
    {
        var contact = Contacts.FirstOrDefault(x => x.Id == e.UserId);
        if (contact is null)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            contact.Connected = true;
        });
    }

    private void ContactDisconnected(object? _, ContactDisconnectedEventArgs e)
    {
        var contact = Contacts.FirstOrDefault(x => x.Id == e.UserId);
        if (contact is null)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            contact.Connected = false;
        });
    }

    private void ContactAdded(object? _, ContactAddedEventArgs e)
    {
        if (!e.IsConnected)
        {
            return;
        }

        var contact = Contacts.FirstOrDefault(x => x.Id == e.UserId);
        if (contact is null)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            contact.Connected = true;
        });
    }

    private void ContactRemoved(object? _, ContactRemovedEventArgs e)
    {
        var contact = Contacts.FirstOrDefault(x => x.Id == e.UserId);
        if (contact is null)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            contact.MessageGroups.Clear();
            Contacts.Remove(contact);
        });
    }

    private async void MessageSent(object? _, MessageSentEventArgs e)
    {
        var localDate = DateOnly.FromDateTime(DateTime.Now);
        var isRecipient = _settingsService.UserId == e.RecipientId;
        var contactId = isRecipient ? e.SenderId : e.RecipientId;

        await Repository.CreateMessageAsync(new Message(e.Id, contactId, isRecipient, e.Text, null, e.SentAt, null));

        var contact = Contacts.FirstOrDefault(x => x.Id == contactId);
        if (contact is null)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            if (SelectedContact == contact)
            {
                var message = new MessageViewModel(
                    e.Id,
                    e.Text,
                    e.SentAt,
                    null,
                    isRecipient,
                    _settingsService.Theme,
                    _settingsService.MessageFontSize);

                var messageSentDate = DateOnly.FromDateTime(message.SentAt);

                if (contact.MessageGroups.Count > 0)
                {
                    var latestGroup = contact.MessageGroups[^1];
                    if (latestGroup!.Date == messageSentDate)
                    {
                        latestGroup.Add(message);
                    }
                    else
                    {
                        var todaysGroup = new DailyMessageGroup(new List<MessageViewModel> { message }, messageSentDate, localDate, _settingsService.MessageFontSize);
                        contact.MessageGroups.Add(todaysGroup);
                    }
                }
                else
                {
                    contact.MessageGroups.Add(new DailyMessageGroup(new List<MessageViewModel> { message }, messageSentDate, localDate, _settingsService.MessageFontSize));
                }
            }
            else
            {
                contact.HasUnreadMessages = true;
            }
        });
    }

    private void MessageSendingFailed(object? _, MessageSendingFailedEventArgs e)
    {
        var contact = Contacts.FirstOrDefault(x => x.Id == e.RecipientId);
        if (contact is null)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            contact.ShowInfoBar("Message sending failed. Please try again in a short moment.", InfoBarSeverity.Error);
        });
    }

    private void MessagePullUpstreamStarted(object? _, MessagePullUpstreamEventArgs e)
    {
        var contact = Contacts.FirstOrDefault(x => x.Id == e.ContactId);
        if (contact is null)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            contact.ShowInfoBar("Streaming messages towards contact..", InfoBarSeverity.Informational);
        });
    }

    private void MessagePullUpstreamCompleted(object? _, MessagePullUpstreamEventArgs e)
    {
        var contact = Contacts.FirstOrDefault(x => x.Id == e.ContactId);
        if (contact is null)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            contact.HideInfoBar();
        });
    }

    private async void MessageUpdated(object? _, MessageUpdatedEventArgs e)
    {
        await Repository.UpdateMessageAsync(e.Id, e.Text, e.EditedAt);

        var contact = Contacts.FirstOrDefault(x => x.Id == e.RecipientId);
        if (contact is null)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            if (SelectedContact == contact)
            {
                var message = contact.MessageGroups.SelectMany(x => x).FirstOrDefault(x => x.Id == e.Id);
                if (message is null)
                {
                    return;
                }

                message.Update(e.Text, e.EditedAt, _settingsService.Theme);
            }

            if (_settingsService.UserId == e.SenderId)
            {
                contact.EditingMessageId = null;
            }
        });
    }

    private void MessageUpdatingFailed(object? _, MessageUpdatingFailedEventArgs e)
    {
        var contact = Contacts.FirstOrDefault(x => x.Id == e.RecipientId);
        if (contact is null)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            if (SelectedContact == contact)
            {
                var message = contact.MessageGroups.SelectMany(x => x).FirstOrDefault(x => x.Id == e.MessageId);
                if (message is null)
                {
                    return;
                }
                message.SetTheme(_settingsService.Theme);
            }

            contact.EditingMessageId = null;

            contact.ShowInfoBar("Message editing failed. Please try again in a short moment.", InfoBarSeverity.Error);
        });
    }

    private void MessagePullCompleted(object? _, MessagePullCompletedEventArgs e)
    {
        if (e.Created == 0)
        {
            return;
        }

        var contact = Contacts.FirstOrDefault(x => x.Id == e.ContactId);
        if (contact is null)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(async () =>
        {
            if (contact == SelectedContact)
            {
                var recentMessages = await Repository.GetRecentMessagesAsync(contact.Id, null);
                contact.SetMessages(recentMessages);
            }
            else
            {
                contact.LastViewed = null;
            }
        });
    }

    private void ContactAddedLocally(object? _, ContactAddedLocallyEventArgs e)
    {
        Contacts.Add(new ContactViewModel(e.UserId, e.Nickname, _settingsService.Theme, _settingsService.MessageFontSize));
    }

    private void ContactEdited(object? _, ContactEditedLocallyEventArgs e)
    {
        var contact = Contacts.FirstOrDefault(x => x.Id == e.UserId);
        if (contact is null)
        {
            return;
        }

        contact.Nickname = e.Nickname;
    }
}
