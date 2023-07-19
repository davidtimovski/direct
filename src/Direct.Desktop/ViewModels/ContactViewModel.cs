using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Direct.Desktop.Storage;
using Direct.Shared.Models;
using Microsoft.UI.Xaml;

namespace Direct.Desktop.ViewModels;

public partial class ContactViewModel : ObservableObject
{
    public ContactDto Contact { get; }

    public Guid? EditingMessageId { get; set; }

    public ContactViewModel(Guid userId, ContactDto contact, IEnumerable<Message> messages, ElementTheme theme, DateOnly localDate)
    {
        Contact = contact;

        Nickname = contact.Nickname;

        var query = from messageVm in messages.Select(x => new MessageViewModel(x.Id, x.Text, x.SentAt, x.EditedAt, x.SenderId == userId, theme))
                    group messageVm by DateOnly.FromDateTime(messageVm.SentAt) into g
                    orderby g.Key
                    select new DailyMessageGroup(g.ToList(), g.Key, localDate);

        MessageGroups = new ObservableCollection<DailyMessageGroup>(query);
    }

    public string Nickname { get; set; }

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
