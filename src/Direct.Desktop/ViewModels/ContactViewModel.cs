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
    private ElementTheme theme;
    private double messageFontSize;

    public Guid Id { get; }
    public Guid? EditingMessageId { get; set; }
    public DateTime? LastViewed { get; set; }

    public ContactViewModel(Guid id, string nickname, ElementTheme theme, double messageFontSize)
    {
        this.theme = theme;
        this.messageFontSize = messageFontSize;

        Id = id;
        Nickname = nickname;
    }

    public void SetMessages(List<Message> messages)
    {
        MessageGroups.Clear();

        var groups = (from messageVm in messages.Select(x => new MessageViewModel(x.Id, x.Text, x.SentAt, x.EditedAt, x.IsRecipient, theme, messageFontSize))
                      orderby messageVm.SentAt
                      group messageVm by DateOnly.FromDateTime(messageVm.SentAt) into g
                      orderby g.Key
                      select new DailyMessageGroup(g, g.Key, localDate: DateOnly.FromDateTime(DateTime.Now), messageFontSize)).ToList();

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

    public void SetThemeAndFontSize(ElementTheme theme, double messageFontSize)
    {
        this.theme = theme;
        this.messageFontSize = messageFontSize;

        foreach (var group in MessageGroups)
        {
            group.LabelFontSize = messageFontSize;

            foreach (var message in group)
            {
                message.SetTheme(theme);
                message.SetFontSize(messageFontSize);
            }
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MessageTextBoxPlaceholder))]
    private string nickname;

    public ObservableCollection<DailyMessageGroup> MessageGroups = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MessageTextBoxPlaceholder))]
    private bool connected;

    [ObservableProperty]
    private bool hasUnreadMessages;

    [ObservableProperty]
    private string messageText = string.Empty;

    public string? MessageTextBoxPlaceholder => Connected ? null : $"{Nickname} is currently offline";

    [ObservableProperty]
    private int messageSelectionStart;

    [ObservableProperty]
    private bool errorBarVisible;

    [ObservableProperty]
    private string? errorBarMessage;
}
