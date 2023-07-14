using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Direct.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

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
    }

    public ObservableCollection<ContactViewModel> Contacts = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSendMessage))]
    private ContactViewModel? selectedContact;

    [ObservableProperty]
    private ElementTheme theme;

    public bool CanSendMessage => SelectedContact is not null;

    public async Task MessageBoxKeyUpAsync(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape && SelectedContact!.EditingMessageId.HasValue)
        {
            var message = SelectedContact!.Messages.First(x => x.Message.Id == SelectedContact!.EditingMessageId);

            SelectedContact!.EditingMessageId = null;
            SelectedContact!.MessageText = string.Empty;

            message?.SetTheme(_storageService.AppData.Theme);
            return;
        }

        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            if (SelectedContact!.EditingMessageId.HasValue)
            {
                await UpdateMessageAsync(SelectedContact!.EditingMessageId.Value);
                return;
            }

            await SendNewMessageAsync();
            return;
        }

        if (e.Key == Windows.System.VirtualKey.Up && SelectedContact!.MessageText == string.Empty)
        {
            var lastSentMessage = SelectedContact!.Messages.OrderByDescending(x => x.Message.SentAtUtc).Where(x => x.Message.UserIsSender).FirstOrDefault();
            if (lastSentMessage is not null)
            {
                SelectedContact!.EditingMessageId = lastSentMessage.Message.Id;
                SelectedContact!.MessageText = lastSentMessage.Text;
                lastSentMessage.SetEditing();
            }
        }
    }

    public void ToggleTheme()
    {
        Theme = Theme == ElementTheme.Light ? ElementTheme.Dark : ElementTheme.Light;

        foreach (var contact in Contacts)
        {
            foreach (var message in contact.Messages)
            {
                message.SetTheme(Theme);
            }
        }

        _storageService.AppData.Theme = Theme;
        _storageService.Store();
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

    private async Task UpdateMessageAsync(Guid id)
    {
        var trimmedMessage = SelectedContact!.MessageText.Trim();
        if (trimmedMessage.Length == 0)
        {
            return;
        }

        await _chatService.UpdateMessageAsync(id, trimmedMessage);
    }

    private void Joined(object? sender, JoinedEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            if (e.Contacts.Count == 0)
            {
                return;
            }

            foreach (var contact in e.Contacts)
            {
                Contacts.Add(new ContactViewModel(contact, _storageService.AppData.Theme));
            }

            SelectedContact = Contacts[0];
        });
    }

    private void ContactJoined(object? sender, ContactJoinedEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            Contacts.Add(new ContactViewModel(e.Contact, _storageService.AppData.Theme));
        });
    }

    private void ContactLeft(object? sender, ContactLeftEventArgs e)
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

    private void MessageSent(object? sender, MessageSentEventArgs e)
    {
        var message = new MessageViewModel(e.Message, _storageService.AppData.Theme);

        if (e.Message.UserIsSender)
        {
            var recipientContact = Contacts.FirstOrDefault(x => x.Contact.Id == e.Message.RecipientId);
            if (recipientContact is not null)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    recipientContact.Messages.Add(message);
                    recipientContact.MessageText = string.Empty;
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
                    senderContact.Messages.Add(message);
                });
            }
        }
    }

    private void MessageUpdated(object? sender, MessageUpdatedEventArgs e)
    {
        if (e.Message.UserIsSender)
        {
            var recipientContact = Contacts.FirstOrDefault(x => x.Contact.Id == e.Message.RecipientId);
            if (recipientContact is not null)
            {
                var message = recipientContact.Messages.FirstOrDefault(x => x.Message.Id == e.Message.Id);
                if (message is not null)
                {
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        message.Text = e.Message.Text;
                        message.SetTheme(_storageService.AppData.Theme);

                        recipientContact.MessageText = string.Empty;
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
                var message = senderContact.Messages.FirstOrDefault(x => x.Message.Id == e.Message.Id);
                if (message is not null)
                {
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        message.Text = e.Message.Text;
                        message.SetTheme(_storageService.AppData.Theme);
                    });
                }
            }
        }
    }
}
