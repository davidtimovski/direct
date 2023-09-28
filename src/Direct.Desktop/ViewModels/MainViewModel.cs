using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Direct.Desktop.Services;
using Direct.Desktop.Storage;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel.DataTransfer;

namespace Direct.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IChatService _chatService;
    private readonly IEventService _eventService;
    private readonly DispatcherQueue _dispatcherQueue;

    public MainViewModel(ISettingsService settingsService, IChatService chatService, IEventService eventService, DispatcherQueue dispatcherQueue)
    {
        _settingsService = settingsService;
        _settingsService.Changed += SettingsChanged;

        _chatService = chatService;
        _chatService.ConnectedContactsRetrieved += ConnectedContactsRetrieved;
        _chatService.Reconnecting += Reconnecting;

        _chatService.ContactConnected += ContactConnected;
        _chatService.ContactDisconnected += ContactDisconnected;
        _chatService.ContactAdded += ContactAdded;
        _chatService.ContactRemoved += ContactRemoved;
        _chatService.MessageSent += MessageSent;
        _chatService.MessageSendingFailed += MessageSendingFailed;
        _chatService.MessageUpdated += MessageUpdated;
        _chatService.MessageUpdatingFailed += MessageUpdatingFailed;

        _eventService = eventService;
        _eventService.ContactAddedLocally += ContactAddedLocally;
        _eventService.ContactEditedLocally += ContactEdited;

        _dispatcherQueue = dispatcherQueue;

        Theme = _settingsService.Theme;
        EmojiPickerVisible = _settingsService.EmojiPickerEnabled;
        SpellCheckEnabled = _settingsService.SpellCheckEnabled;
        UserId = _settingsService.UserId!.Value.ToString("N");

        ConnectionStatus = new(_chatService, dispatcherQueue);
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
        if (contacts.Count > 0)
        {
            var messages = await Repository.GetAllMessagesAsync();

            var contactViewModels = new List<ContactViewModel>(contacts.Count);
            foreach (var contact in contacts)
            {
                var contactMessages = messages.Where(x => x.SenderId == contact.Id || x.RecipientId == contact.Id);
                contactViewModels.Add(new ContactViewModel(
                    _settingsService.UserId!.Value,
                    contact.Id,
                    contact.Nickname,
                    contactMessages,
                    _settingsService.Theme,
                    _settingsService.MessageFontSize,
                    localDate: DateOnly.FromDateTime(DateTime.Now)));
            }

            var orderedContacts = contactViewModels.OrderByDescending(
                c => c.MessageGroups
                    .SelectMany(x => x)
                    .OrderByDescending(m => m.SentAt)
                    .Select(m => m.SentAt)
                    .FirstOrDefault()
            );

            foreach (var contactViewModel in orderedContacts)
            {
                Contacts.Add(contactViewModel);
            }
        }

        var contactIds = contacts.Select(x => x.Id).ToHashSet();
        return await _chatService.ConnectAsync(_settingsService.UserId!.Value, contactIds);
    }

    public void SelectedContactChanged()
    {
        if (SelectedContact is not null)
        {
            SelectedContact.HasUnreadMessages = false;
        }
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
            await _chatService.UpdateMessageAsync(SelectedContact!.EditingMessageId.Value, SelectedContact!.UserId, trimmedMessage);
            return;
        }

        await _chatService.SendMessageAsync(SelectedContact!.UserId, trimmedMessage);
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
        await Repository.DeleteContactAsync(SelectedContact!.UserId, deleteMessages);
        await _chatService.RemoveContactAsync(SelectedContact!.UserId);
    }

    private void SelectLastSentMessageForUpdate()
    {
        var lastSentMessage = SelectedContact!.MessageGroups.SelectMany(x => x).Where(x => x.UserIsSender).OrderByDescending(x => x.SentAt).FirstOrDefault();
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
            foreach (var group in contact.MessageGroups)
            {
                group.LabelFontSize = e.MessageFontSize;

                foreach (var message in group)
                {
                    message.SetTheme(Theme);
                    message.SetFontSize(e.MessageFontSize);
                }
            }
        }

        Theme = e.Theme;
        SpellCheckEnabled = e.SpellCheckEnabled;
        EmojiPickerVisible = e.EmojiPickerEnabled;
    }

    private void ConnectedContactsRetrieved(object? eee, ConnectedContactsRetrievedEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            var connectedContacts = Contacts.Where(x => e.ConnectedUserIds.Contains(x.UserId)).ToList();
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
        var contact = Contacts.FirstOrDefault(x => x.UserId == e.UserId);
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
        var contact = Contacts.FirstOrDefault(x => x.UserId == e.UserId);
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

        var contact = Contacts.FirstOrDefault(x => x.UserId == e.UserId);
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
        var contact = Contacts.FirstOrDefault(x => x.UserId == e.UserId);
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
        var userIsSender = e.SenderId == _settingsService.UserId;

        await Repository.CreateMessageAsync(new Message
        {
            Id = e.Id,
            SenderId = e.SenderId,
            RecipientId = e.RecipientId,
            Text = e.Text,
            SentAt = e.SentAt
        });

        _dispatcherQueue.TryEnqueue(() =>
        {
            var message = new MessageViewModel(
                e.Id,
                e.Text,
                e.SentAt,
                null,
                userIsSender,
                _settingsService.Theme,
                _settingsService.MessageFontSize);

            if (userIsSender)
            {
                var recipientContact = Contacts.FirstOrDefault(x => x.UserId == e.RecipientId);
                if (recipientContact is null)
                {
                    return;
                }

                var messageSentDate = DateOnly.FromDateTime(message.SentAt);

                if (recipientContact.MessageGroups.Count > 0)
                {
                    var latestGroup = recipientContact.MessageGroups[^1];
                    if (latestGroup!.Date == messageSentDate)
                    {
                        latestGroup.Add(message);
                    }
                    else
                    {
                        recipientContact.MessageGroups.Add(new DailyMessageGroup(new List<MessageViewModel> { message }, messageSentDate, localDate, _settingsService.MessageFontSize));
                    }
                }
                else
                {
                    recipientContact.MessageGroups.Add(new DailyMessageGroup(new List<MessageViewModel> { message }, messageSentDate, localDate, _settingsService.MessageFontSize));
                }
            }
            else
            {
                var senderContact = Contacts.FirstOrDefault(x => x.UserId == e.SenderId);
                if (senderContact is null)
                {
                    return;
                }

                var messageSentDate = DateOnly.FromDateTime(message.SentAt);

                if (senderContact.MessageGroups.Count > 0)
                {
                    var latestGroup = senderContact.MessageGroups[^1];
                    if (latestGroup!.Date == messageSentDate)
                    {
                        latestGroup.Add(message);
                    }
                    else
                    {
                        var todaysGroup = new DailyMessageGroup(new List<MessageViewModel> { message }, messageSentDate, localDate, _settingsService.MessageFontSize);
                        senderContact.MessageGroups.Add(todaysGroup);
                    }
                }
                else
                {
                    senderContact.MessageGroups.Add(new DailyMessageGroup(new List<MessageViewModel> { message }, messageSentDate, localDate, _settingsService.MessageFontSize));
                }

                if (senderContact != SelectedContact)
                {
                    senderContact.HasUnreadMessages = true;
                }
            }
        });
    }

    private void MessageSendingFailed(object? _, MessageSendingFailedEventArgs e)
    {
        var contact = Contacts.FirstOrDefault(x => x.UserId == e.RecipientId);
        if (contact is null)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            ShowErrorBar(contact, "Message sending failed. Please try again in a short moment.");
        });
    }

    private async void MessageUpdated(object? _, MessageUpdatedEventArgs e)
    {
        await Repository.UpdateMessageAsync(e.Id, e.Text, e.EditedAt);

        if (e.SenderId == _settingsService.UserId)
        {
            var recipientContact = Contacts.FirstOrDefault(x => x.UserId == e.RecipientId);
            if (recipientContact is null)
            {
                return;
            }

            var message = recipientContact.MessageGroups.SelectMany(x => x).FirstOrDefault(x => x.Id == e.Id);
            if (message is null)
            {
                return;
            }

            _dispatcherQueue.TryEnqueue(() =>
            {
                message.Update(e.Text, e.EditedAt, _settingsService.Theme);

                recipientContact.EditingMessageId = null;
            });
        }
        else
        {
            var senderContact = Contacts.FirstOrDefault(x => x.UserId == e.SenderId);
            if (senderContact is null)
            {
                return;
            }

            var message = senderContact.MessageGroups.SelectMany(x => x).FirstOrDefault(x => x.Id == e.Id);
            if (message is null)
            {
                return;
            }

            _dispatcherQueue.TryEnqueue(() =>
            {
                message.Update(e.Text, e.EditedAt, _settingsService.Theme);
            });
        }
    }

    private void MessageUpdatingFailed(object? _, MessageUpdatingFailedEventArgs e)
    {
        var contact = Contacts.FirstOrDefault(x => x.UserId == e.RecipientId);
        if (contact is null)
        {
            return;
        }

        var message = contact.MessageGroups.SelectMany(x => x).FirstOrDefault(x => x.Id == e.MessageId);
        if (message is null)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            message.SetTheme(_settingsService.Theme);
            contact.EditingMessageId = null;

            ShowErrorBar(contact, "Message editing failed. Please try again in a short moment.");
        });
    }

    private async void ContactAddedLocally(object? sender, ContactAddedLocallyEventArgs e)
    {
        var contactMessages = await Repository.GetMessagesAsync(e.UserId);
        Contacts.Add(new ContactViewModel(
            _settingsService.UserId!.Value,
            e.UserId,
            e.Nickname,
            contactMessages,
            _settingsService.Theme,
            _settingsService.MessageFontSize,
            localDate: DateOnly.FromDateTime(DateTime.Now)));
    }

    private void ContactEdited(object? sender, ContactEditedLocallyEventArgs e)
    {
        var contact = Contacts.FirstOrDefault(x => x.UserId == e.UserId);
        if (contact is null)
        {
            return;
        }

        contact.Nickname = e.Nickname;
    }

    private static void ShowErrorBar(ContactViewModel contact, string message)
    {
        contact.ErrorBarMessage = message;
        contact.ErrorBarVisible = true;

        var dispatcherTimer = new DispatcherTimer();
        dispatcherTimer.Tick += (object? sender, object e) =>
        {
            contact.ErrorBarVisible = false;
            contact.ErrorBarMessage = null;
            dispatcherTimer.Stop();
        };
        dispatcherTimer.Interval = TimeSpan.FromSeconds(5);
        dispatcherTimer.Start();
    }
}
