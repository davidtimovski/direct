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

        _chatService = chatService;
        _chatService.Connected += Joined;
        _chatService.ContactConnected += ContactConnected;
        _chatService.ContactDisconnected += ContactDisconnected;
        _chatService.AddedContactIsConnected += AddedContactIsConnected;
        _chatService.MessageSent += MessageSent;
        _chatService.MessageUpdated += MessageUpdated;

        _eventService = eventService;
        _eventService.ContactAdded += ContactAdded;

        _dispatcherQueue = dispatcherQueue;

        userId = _settingsService.UserId.ToString();
        Theme = _settingsService.Theme;
        SelectedTheme = Theme.ToString();
    }

    [ObservableProperty]
    private bool connected;

    [ObservableProperty]
    private string userId = string.Empty;

    [ObservableProperty]
    private string nickname = string.Empty;

    [ObservableProperty]
    private ElementTheme theme;

    [ObservableProperty]
    private string selectedTheme;

    public ObservableCollection<ContactViewModel> Contacts = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MessageBoxIsVisible))]
    private ContactViewModel? selectedContact;

    public bool MessageBoxIsVisible => SelectedContact is not null;

    public void SelectedContactChanged()
    {
        SelectedContact!.HasUnreadMessages = false;
    }

    public async Task MessageBoxKeyUpAsync(object _, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            SelectedContact!.MessageTextIsReadOnly = true;

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

    public void ThemeChanged()
    {
        Theme = SelectedTheme == ElementTheme.Light.ToString()
            ? ElementTheme.Light
            : ElementTheme.Dark;

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

        _settingsService.Theme = Theme;
    }

    private async Task SendNewMessageAsync()
    {
        var trimmedMessage = SelectedContact!.MessageText.Trim();
        if (trimmedMessage.Length == 0)
        {
            return;
        }

        await _chatService.SendMessageAsync(SelectedContact.UserId, trimmedMessage);
    }

    private void SelectLastSentMessageForUpdate()
    {
        var lastSentMessage = SelectedContact!.MessageGroups.SelectMany(x => x).Where(x => x.UserIsSender).OrderByDescending(x => x.SentAt).FirstOrDefault();
        if (lastSentMessage is not null)
        {
            SelectedContact!.EditingMessageId = lastSentMessage.Id;
            SelectedContact!.MessageText = lastSentMessage.Text;
            SelectedContact!.MessageSelectionStart = lastSentMessage.Text.Length;
            lastSentMessage.SetEditing();
        }
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

    private void Joined(object? _, ConnectedEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(async () =>
        {
            Connected = true;
            Nickname = _settingsService.Nickname;

            var contacts = await Repository.GetAllContactsAsync();
            if (contacts.Count == 0)
            {
                return;
            }

            var messages = await Repository.GetAllMessagesAsync();

            var contactViewModels = new List<ContactViewModel>(contacts.Count);
            foreach (var contact in contacts)
            {
                var contactMessages = messages.Where(x => x.SenderId == contact.Id || x.RecipientId == contact.Id);
                var connected = e.ConnectedUserIds.Contains(contact.Id);
                contactViewModels.Add(new ContactViewModel(_settingsService.UserId, contact.Id, contact.Nickname, contactMessages, connected, _settingsService.Theme, localDate: DateOnly.FromDateTime(DateTime.Now)));
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
        });
    }

    private void ContactConnected(object? _, ContactConnectedEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            var contact = Contacts.FirstOrDefault(x => x.UserId == e.UserId);
            if (contact is not null)
            {
                contact.Connected = true;
            }
        });
    }

    private void AddedContactIsConnected(object? _, AddedContactIsConnectedEventArgs e)
    {
        if (!e.IsConnected)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            var contact = Contacts.FirstOrDefault(x => x.UserId == e.UserId);
            if (contact is not null)
            {
                contact.Connected = true;
            }
        });
    }

    private void ContactDisconnected(object? _, ContactDisconnectedEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            var contact = Contacts.FirstOrDefault(x => x.UserId == e.UserId);
            if (contact is not null)
            {
                contact.Connected = false;
            }
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
                if (recipientContact is not null)
                {
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
                    recipientContact.MessageTextIsReadOnly = false;
                }
            }
            else
            {
                var senderContact = Contacts.FirstOrDefault(x => x.UserId == e.Message.SenderId);
                if (senderContact is not null)
                {
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
            }
        });
    }

    private async void MessageUpdated(object? _, MessageUpdatedEventArgs e)
    {
        await Repository.UpdateMessageAsync(e.Message.Id, e.Message.Text, e.Message.EditedAtUtc.ToLocalTime());

        if (e.Message.SenderId == _settingsService.UserId)
        {
            var recipientContact = Contacts.FirstOrDefault(x => x.UserId == e.Message.RecipientId);
            if (recipientContact is not null)
            {
                var message = recipientContact.MessageGroups.SelectMany(x => x).FirstOrDefault(x => x.Id == e.Message.Id);
                if (message is not null)
                {
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        message.Update(e.Message.Text, e.Message.EditedAtUtc.ToLocalTime(), _settingsService.Theme);

                        recipientContact.MessageText = string.Empty;
                        recipientContact.MessageTextIsReadOnly = false;
                        recipientContact.EditingMessageId = null;
                    });
                }
            }
        }
        else
        {
            var senderContact = Contacts.FirstOrDefault(x => x.UserId == e.Message.SenderId);
            if (senderContact is not null)
            {
                var message = senderContact.MessageGroups.SelectMany(x => x).FirstOrDefault(x => x.Id == e.Message.Id);
                if (message is not null)
                {
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        message.Update(e.Message.Text, e.Message.EditedAtUtc.ToLocalTime(), _settingsService.Theme);
                    });
                }
            }
        }
    }

    private async void ContactAdded(object? sender, ContactAddedEventArgs e)
    {
        var contactMessages = await Repository.GetMessagesAsync(e.UserId);
        Contacts.Add(new ContactViewModel(_settingsService.UserId, e.UserId, e.Nickname, contactMessages, false, _settingsService.Theme, localDate: DateOnly.FromDateTime(DateTime.Now)));
    }
}
