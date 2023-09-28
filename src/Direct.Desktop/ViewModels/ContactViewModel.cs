using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Direct.Desktop.Storage.Entities;
using Microsoft.UI.Xaml;

namespace Direct.Desktop.ViewModels;

public partial class ContactViewModel : ObservableObject
{
    public Guid Id { get; }
    public Guid? EditingMessageId { get; set; }
    public DateTime? LastViewed { get; set; }

    public ContactViewModel(Guid id, string nickname)
    {
        Id = id;
        Nickname = nickname;
    }

    public void AddMessages(IEnumerable<Message> messages, ElementTheme theme, double messageFontSize, DateOnly localDate)
    {
        var groups = (from messageVm in messages.Select(x => new MessageViewModel(x.Id, x.Text, x.SentAt, x.EditedAt, x.IsRecipient, theme, messageFontSize))
                      orderby messageVm.SentAt
                      group messageVm by DateOnly.FromDateTime(messageVm.SentAt) into g
                      orderby g.Key
                      select new DailyMessageGroup(g, g.Key, localDate, messageFontSize)).ToList();

        if (MessageGroups.Count == 0)
        {
            foreach (var group in groups)
            {
                MessageGroups.Add(group);
            }
        }
        else
        {
            foreach (var group in groups)
            {
                var dayGroup = MessageGroups.FirstOrDefault(x => x.Date == group.Date);
                if (dayGroup is null)
                {
                    MessageGroups.Add(group);
                }
                else
                {
                    foreach (var message in group)
                    {
                        dayGroup.Add(message);
                    }
                }
            }
        }
    }

    [ObservableProperty]
    private string nickname;

    public ObservableCollection<DailyMessageGroup> MessageGroups = new();

    [ObservableProperty]
    private bool connected;

    [ObservableProperty]
    private bool hasUnreadMessages;

    [ObservableProperty]
    private string messageText = string.Empty;

    [ObservableProperty]
    private int messageSelectionStart;

    [ObservableProperty]
    private bool errorBarVisible;

    [ObservableProperty]
    private string? errorBarMessage;
}
