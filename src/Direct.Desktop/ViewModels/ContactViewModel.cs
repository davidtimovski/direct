using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Direct.Desktop.Storage.Entities;
using Direct.Desktop.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Direct.Desktop.ViewModels;

public partial class ContactViewModel : ObservableObject
{
    private ElementTheme theme;
    private double messageFontSize;

    public Guid Id { get; }
    public Guid? EditingMessageId { get; set; }
    public DateTime? LastViewed { get; set; }

    public ContactViewModel(Guid id, string nickname, string? profileImage, ElementTheme theme, double messageFontSize)
    {
        this.theme = theme;
        this.messageFontSize = messageFontSize;

        Id = id;
        Nickname = nickname;
        ProfileImageSource = ProfileImageUtil.GetSource(profileImage);
    }

    public void SetMessages(IReadOnlyList<Message> messages)
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

    public void ShowInfoBar(string message, InfoBarSeverity severity, double? secondsVisible = 6)
    {
        InfoBarMessage = message;
        InfoBarVisible = true;
        InfoBarSeverity = severity;

        if (secondsVisible.HasValue)
        {
            var dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += (object? sender, object e) =>
            {
                HideInfoBar();
                dispatcherTimer.Stop();
            };
            dispatcherTimer.Interval = TimeSpan.FromSeconds(secondsVisible.Value);
            dispatcherTimer.Start();
        }
    }

    public void HideInfoBar()
    {
        InfoBarVisible = false;
        InfoBarMessage = null;
    }

    public void SetProfileImage(string profileImage)
    {
        ProfileImageSource = ProfileImageUtil.GetSource(profileImage);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MessageTextBoxPlaceholder))]
    private string nickname;

    [ObservableProperty]
    private string profileImageSource;

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
    private bool infoBarVisible;

    [ObservableProperty]
    private string? infoBarMessage;

    [ObservableProperty]
    private InfoBarSeverity infoBarSeverity;
}
