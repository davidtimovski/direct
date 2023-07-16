using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Direct.Shared.Models;
using Microsoft.UI.Xaml;

namespace Direct.ViewModels;

public partial class ContactViewModel : ObservableObject
{
    public ContactDto Contact { get; }

    public Guid? EditingMessageId { get; set; }

    public ContactViewModel(ContactDto contact, ElementTheme theme, DateOnly localDate)
    {
        Contact = contact;

        Nickname = contact.Nickname;
        ImageUri = contact.ImageUri;

        var query = from messageVm in contact.Messages.Select(x => new MessageViewModel(x, theme))
                    group messageVm by DateOnly.FromDateTime(messageVm.Message.SentAtUtc.ToLocalTime()) into g
                    orderby g.Key
                    select new DailyMessageGroup(g.ToList(), g.Key, localDate);

        MessageGroups = new ObservableCollection<DailyMessageGroup>(query);
    }

    public string Nickname { get; set; }
    public string ImageUri { get; set; }

    public ObservableCollection<DailyMessageGroup> MessageGroups;

    [ObservableProperty]
    private bool hasUnreadMessages;

    [ObservableProperty]
    private string messageText = string.Empty;

    [ObservableProperty]
    private bool messageTextIsReadOnly;

    [ObservableProperty]
    private int messageSelectionStart;
}
