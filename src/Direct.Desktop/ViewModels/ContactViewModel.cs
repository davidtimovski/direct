using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Direct.Desktop.Storage;
using Microsoft.UI.Xaml;

namespace Direct.Desktop.ViewModels;

public partial class ContactViewModel : ObservableObject
{
    public Guid UserId { get; }

    public Guid? EditingMessageId { get; set; }

    public ContactViewModel(Guid userId, Guid contactUserId, string nickname, IEnumerable<Message> messages, bool connected, ElementTheme theme, DateOnly localDate)
    {
        UserId = contactUserId;
        Nickname = nickname;

        var query = from messageVm in messages.Select(x => new MessageViewModel(x.Id, x.Text, x.SentAt, x.EditedAt, x.SenderId == userId, theme))
                    group messageVm by DateOnly.FromDateTime(messageVm.SentAt) into g
                    orderby g.Key
                    select new DailyMessageGroup(g.ToList(), g.Key, localDate);

        MessageGroups = new ObservableCollection<DailyMessageGroup>(query);

        Connected = connected;
    }

    [ObservableProperty]
    private string nickname;

    public ObservableCollection<DailyMessageGroup> MessageGroups;

    [ObservableProperty]
    private bool connected;

    [ObservableProperty]
    private bool hasUnreadMessages;

    [ObservableProperty]
    private string messageText = string.Empty;

    [ObservableProperty]
    private bool messageTextIsReadOnly;

    [ObservableProperty]
    private int messageSelectionStart;

    [ObservableProperty]
    private bool errorBarVisible;

    [ObservableProperty]
    private string? errorBarMessage;
}
