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
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using VirtualKey = Windows.System.VirtualKey;

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
        _settingsService.ThemeChanged += ThemeChanged;

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
        _eventService.ContactEditedLocally += ContactEditedLocally;

        _dispatcherQueue = dispatcherQueue;

        Theme = _settingsService.Theme;
        UserId = _settingsService.UserId!.Value.ToString("N");

        ConnectionStatus = new(_chatService, dispatcherQueue);
    }

    [ObservableProperty]
    private ElementTheme theme;

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
                contactViewModels.Add(new ContactViewModel(_settingsService.UserId!.Value, contact.Id, contact.Nickname, contactMessages, _settingsService.Theme, localDate: DateOnly.FromDateTime(DateTime.Now)));
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

    public async void MessageBoxKeyUpAsync(object _, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            SelectedContact!.SendingMessage = true;

            if (SelectedContact!.EditingMessageId.HasValue)
            {
                await UpdateMessageAsync(SelectedContact!.EditingMessageId.Value, SelectedContact!.UserId);
                return;
            }

            await SendNewMessageAsync();
            return;
        }

        if (e.Key == VirtualKey.Up
            && !SelectedContact!.EditingMessageId.HasValue
            && SelectedContact!.MessageText == string.Empty)
        {
            SelectLastSentMessageForUpdate();
            return;
        }

        if (e.Key == VirtualKey.Escape
            && SelectedContact!.EditingMessageId.HasValue)
        {
            DeselectLastSentMessage();
        }
    }

    public void CopyUserID()
    {
        var package = new DataPackage();
        package.SetText(UserId);
        Clipboard.SetContent(package);
    }

    public async void DeleteContactAsync()
    {
        await Repository.DeleteContactAsync(SelectedContact!.UserId);
        await _chatService.RemoveContactAsync(SelectedContact!.UserId);
    }

    private async Task SendNewMessageAsync()
    {
        var trimmedMessage = SelectedContact!.MessageText.Trim();
        if (trimmedMessage.Length == 0)
        {
            return;
        }

        await _chatService.SendMessageAsync(SelectedContact!.UserId, trimmedMessage);
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

    private async Task UpdateMessageAsync(Guid id, Guid recipientId)
    {
        var trimmedMessage = SelectedContact!.MessageText.Trim();
        if (trimmedMessage.Length == 0)
        {
            return;
        }

        await _chatService.UpdateMessageAsync(id, recipientId, trimmedMessage);
    }

    private void ThemeChanged(object? _, ThemeChangedEventArgs e)
    {
        foreach (var contact in Contacts)
        {
            foreach (var group in contact.MessageGroups)
            {
                foreach (var message in group)
                {
                    message.SetTheme(Theme);
                }
            }
        }

        Theme = e.Theme;
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

    private async void MessageSent(object? _, MessageSentEventArgs e)
    {
        var localDate = DateOnly.FromDateTime(DateTime.Now);
        var userIsSender = e.Message.SenderId == _settingsService.UserId;

        await Repository.CreateMessageAsync(new Message
        {
            Id = e.Message.Id,
            SenderId = e.Message.SenderId,
            RecipientId = e.Message.RecipientId,
            Text = e.Message.Text,
            SentAt = e.Message.SentAtUtc.ToLocalTime()
        });

        _dispatcherQueue.TryEnqueue(() =>
        {
            var message = new MessageViewModel(
                e.Message.Id,
                e.Message.Text,
                e.Message.SentAtUtc.ToLocalTime(),
                null,
                userIsSender,
                _settingsService.Theme);

            if (userIsSender)
            {
                var recipientContact = Contacts.FirstOrDefault(x => x.UserId == e.Message.RecipientId);
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
                        recipientContact.MessageGroups.Add(new DailyMessageGroup(new List<MessageViewModel> { message }, messageSentDate, localDate));
                    }
                }
                else
                {
                    recipientContact.MessageGroups.Add(new DailyMessageGroup(new List<MessageViewModel> { message }, messageSentDate, localDate));
                }

                recipientContact.MessageText = string.Empty;
                recipientContact.SendingMessage = false;
            }
            else
            {
                var senderContact = Contacts.FirstOrDefault(x => x.UserId == e.Message.SenderId);
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
                        var todaysGroup = new DailyMessageGroup(new List<MessageViewModel> { message }, messageSentDate, localDate);
                        senderContact.MessageGroups.Add(todaysGroup);
                    }
                }
                else
                {
                    senderContact.MessageGroups.Add(new DailyMessageGroup(new List<MessageViewModel> { message }, messageSentDate, localDate));
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
            contact.MessageText = string.Empty;
            contact.SendingMessage = false;

            ShowErrorBar(contact, "Message sending failed. Please try again in a short moment.");
        });
    }

    private async void MessageUpdated(object? _, MessageUpdatedEventArgs e)
    {
        await Repository.UpdateMessageAsync(e.Message.Id, e.Message.Text, e.Message.EditedAtUtc.ToLocalTime());

        if (e.Message.SenderId == _settingsService.UserId)
        {
            var recipientContact = Contacts.FirstOrDefault(x => x.UserId == e.Message.RecipientId);
            if (recipientContact is null)
            {
                return;
            }

            var message = recipientContact.MessageGroups.SelectMany(x => x).FirstOrDefault(x => x.Id == e.Message.Id);
            if (message is null)
            {
                return;
            }

            _dispatcherQueue.TryEnqueue(() =>
            {
                message.Update(e.Message.Text, e.Message.EditedAtUtc.ToLocalTime(), _settingsService.Theme);

                recipientContact.MessageText = string.Empty;
                recipientContact.SendingMessage = false;
                recipientContact.EditingMessageId = null;
            });
        }
        else
        {
            var senderContact = Contacts.FirstOrDefault(x => x.UserId == e.Message.SenderId);
            if (senderContact is null)
            {
                return;
            }

            var message = senderContact.MessageGroups.SelectMany(x => x).FirstOrDefault(x => x.Id == e.Message.Id);
            if (message is null)
            {
                return;
            }

            _dispatcherQueue.TryEnqueue(() =>
            {
                message.Update(e.Message.Text, e.Message.EditedAtUtc.ToLocalTime(), _settingsService.Theme);
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
            contact.MessageText = string.Empty;
            contact.SendingMessage = false;
            contact.EditingMessageId = null;

            ShowErrorBar(contact, "Message editing failed. Please try again in a short moment.");
        });
    }

    private async void ContactAddedLocally(object? sender, ContactAddedLocallyEventArgs e)
    {
        var contactMessages = await Repository.GetMessagesAsync(e.UserId);
        Contacts.Add(new ContactViewModel(_settingsService.UserId!.Value, e.UserId, e.Nickname, contactMessages, _settingsService.Theme, localDate: DateOnly.FromDateTime(DateTime.Now)));
    }

    private void ContactEditedLocally(object? sender, ContactEditedLocallyEventArgs e)
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
