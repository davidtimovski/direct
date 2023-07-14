using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Direct.Shared.Models;
using Microsoft.UI.Xaml;

namespace Direct.ViewModels;

public partial class ContactViewModel : ObservableObject
{
    public ContactDto Contact { get; }

    public Guid? EditingMessageId { get; set; }

    public ContactViewModel(ContactDto contact, ElementTheme theme)
    {
        Contact = contact;

        Nickname = contact.Nickname;
        ImageUri = contact.ImageUri;

        foreach (var message in contact.Messages)
        {
            Messages.Add(new MessageViewModel(message, theme));
        }
    }

    public string Nickname { get; set; }
    public string ImageUri { get; set; }

    public ObservableCollection<MessageViewModel> Messages = new();

    [ObservableProperty]
    private string messageText = string.Empty;
}
