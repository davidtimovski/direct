using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Direct.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using VirtualKey = Windows.System.VirtualKey;

namespace Direct.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IStorageService _storageService;
    private readonly IChatService _chatService;
    private readonly DispatcherQueue _dispatcherQueue;

    public MainViewModel(IStorageService storageService, IChatService chatService, DispatcherQueue dispatcherQueue)
    {
        _storageService = storageService;

        _chatService = chatService;
        _chatService.Joined += Joined;
        _chatService.ContactJoined += ContactJoined;
        _chatService.ContactLeft += ContactLeft;
        _chatService.MessageSent += MessageSent;
        _chatService.MessageUpdated += MessageUpdated;

        _dispatcherQueue = dispatcherQueue;

        Theme = _storageService.AppData.Theme;
        SelectedTheme = Theme.ToString();
    }

    public ObservableCollection<ContactViewModel> Contacts = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MessageBoxIsVisible))]
    private ContactViewModel? selectedContact;

    [ObservableProperty]
    private ElementTheme theme;

    [ObservableProperty]
    private string selectedTheme;

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
                await UpdateMessageAsync(SelectedContact!.EditingMessageId.Value);
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

        _storageService.AppData.Theme = Theme;
        _storageService.Save();
    }

    private async Task SendNewMessageAsync()
    {
        var trimmedMessage = SelectedContact!.MessageText.Trim();
        if (trimmedMessage.Length == 0)
        {
            return;
        }

        await _chatService.SendMessageAsync(SelectedContact.Contact.Id, trimmedMessage);
    }

    private void SelectLastSentMessageForUpdate()
    {
        var lastSentMessage = SelectedContact!.MessageGroups.SelectMany(x => x).Where(x => x.Message.UserIsSender).OrderByDescending(x => x.Message.SentAtUtc).FirstOrDefault();
        if (lastSentMessage is not null)
        {
            SelectedContact!.EditingMessageId = lastSentMessage.Message.Id;
            SelectedContact!.MessageText = lastSentMessage.Text;
            SelectedContact!.MessageSelectionStart = lastSentMessage.Text.Length;
            lastSentMessage.SetEditing();
        }
    }

    private void DeselectLastSentMessage()
    {
        var message = SelectedContact!.MessageGroups.SelectMany(x => x).First(x => x.Message.Id == SelectedContact!.EditingMessageId);

        SelectedContact!.EditingMessageId = null;
        SelectedContact!.MessageText = string.Empty;

        message?.SetTheme(_storageService.AppData.Theme);
    }

    private async Task UpdateMessageAsync(Guid id)
    {
        var trimmedMessage = SelectedContact!.MessageText.Trim();
        if (trimmedMessage.Length == 0)
        {
            return;
        }

        await _chatService.UpdateMessageAsync(id, trimmedMessage);
    }

    private void Joined(object? _, JoinedEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            if (e.Contacts.Count == 0)
            {
                return;
            }

            foreach (var contact in e.Contacts)
            {
                Contacts.Add(new ContactViewModel(contact, _storageService.AppData.Theme, localDate: DateOnly.FromDateTime(DateTime.Now)));
            }

            SelectedContact = Contacts[0];
        });
    }

    private void ContactJoined(object? _, ContactJoinedEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            Contacts.Add(new ContactViewModel(e.Contact, _storageService.AppData.Theme, localDate: DateOnly.FromDateTime(DateTime.Now)));
        });
    }

    private void ContactLeft(object? _, ContactLeftEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            var contact = Contacts.FirstOrDefault(x => x.Contact.Id == e.UserId);
            if (contact is not null)
            {
                Contacts.Remove(contact);
            }
        });
    }

    private void MessageSent(object? _, MessageSentEventArgs e)
    {
        var localDate = DateOnly.FromDateTime(DateTime.Now);
        var message = new MessageViewModel(e.Message, _storageService.AppData.Theme);

        if (e.Message.UserIsSender)
        {
            var recipientContact = Contacts.FirstOrDefault(x => x.Contact.Id == e.Message.RecipientId);
            if (recipientContact is not null)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    var latestGroup = recipientContact.MessageGroups[^1];
                    var messageSentDate = DateOnly.FromDateTime(message.Message.SentAtUtc.ToLocalTime());

                    if (latestGroup!.Date == messageSentDate)
                    {
                        latestGroup.Add(message);
                    }
                    else
                    {
                        var todaysGroup = new DailyMessageGroup(new List<MessageViewModel> { message }, messageSentDate, localDate);
                        recipientContact.MessageGroups.Add(todaysGroup);
                    }

                    recipientContact.MessageText = string.Empty;
                    recipientContact.MessageTextIsReadOnly = false;
                });
            }
        }
        else
        {
            var senderContact = Contacts.FirstOrDefault(x => x.Contact.Id == e.Message.SenderId);
            if (senderContact is not null)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    var latestGroup = senderContact.MessageGroups[^1];
                    var messageSentDate = DateOnly.FromDateTime(message.Message.SentAtUtc.ToLocalTime());

                    if (latestGroup!.Date == messageSentDate)
                    {
                        latestGroup.Add(message);
                    }
                    else
                    {
                        var todaysGroup = new DailyMessageGroup(new List<MessageViewModel> { message }, messageSentDate, localDate);
                        senderContact.MessageGroups.Add(todaysGroup);
                    }

                    if (senderContact != SelectedContact)
                    {
                        senderContact.HasUnreadMessages = true;
                    }
                });
            }
        }
    }

    private void MessageUpdated(object? _, MessageUpdatedEventArgs e)
    {
        if (e.Message.UserIsSender)
        {
            var recipientContact = Contacts.FirstOrDefault(x => x.Contact.Id == e.Message.RecipientId);
            if (recipientContact is not null)
            {
                var message = recipientContact.MessageGroups.SelectMany(x => x).FirstOrDefault(x => x.Message.Id == e.Message.Id);
                if (message is not null)
                {
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        message.Update(e.Message.Text, e.Message.EditedAtUtc!.Value, _storageService.AppData.Theme);

                        recipientContact.MessageText = string.Empty;
                        recipientContact.MessageTextIsReadOnly = false;
                        recipientContact.EditingMessageId = null;
                    });
                }
            }
        }
        else
        {
            var senderContact = Contacts.FirstOrDefault(x => x.Contact.Id == e.Message.SenderId);
            if (senderContact is not null)
            {
                var message = senderContact.MessageGroups.SelectMany(x => x).FirstOrDefault(x => x.Message.Id == e.Message.Id);
                if (message is not null)
                {
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        message.Update(e.Message.Text, e.Message.EditedAtUtc!.Value, _storageService.AppData.Theme);
                    });
                }
            }
        }
    }
}
