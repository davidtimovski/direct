using System;
using System.Collections.Generic;
using Chat.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using Direct.Shared.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Direct.ViewModels;

public partial class MessageViewModel : ObservableObject
{
    private const string TimeFormat = "HH:mm:ss";

    private static readonly SolidColorBrush UserMessageBackgroundBrush = ResourceUtil.GetBrush("HighlightBrush");
    private static readonly SolidColorBrush UserMessageForegroundBrush = new(Colors.White);
    private static readonly Dictionary<ElementTheme, SolidColorBrush> SenderMessageBackgroundBrushes = new()
    {
        { ElementTheme.Light, ResourceUtil.GetBrush("SenderMessageBackgroundBrushLight") },
        { ElementTheme.Dark, ResourceUtil.GetBrush("SenderMessageBackgroundBrushDark") },
    };
    private static readonly Dictionary<ElementTheme, SolidColorBrush> SenderMessageForegroundBrushes = new()
    {
        { ElementTheme.Light, ResourceUtil.GetBrush("SenderMessageForegroundBrushLight") },
        { ElementTheme.Dark, ResourceUtil.GetBrush("SenderMessageForegroundBrushDark") },
    };
    private static readonly SolidColorBrush MessageEditingBackgroundBrush = ResourceUtil.GetBrush("MessageEditingBackgroundBrush");
    private static readonly SolidColorBrush MessageEditingForegroundBrush = new(Colors.White);

    public MessageDto Message { get; }

    public MessageViewModel(MessageDto message, ElementTheme theme)
    {
        Message = message;

        Text = message.Text;
        SentAtFormatted = FormatDate(message.SentAtUtc);
        Edited = message.EditedAtUtc.HasValue;
        EditedAt = message.EditedAtUtc.HasValue ? FormatEditedAt(message.EditedAtUtc.Value) : string.Empty;

        if (message.UserIsSender)
        {
            Alignment = HorizontalAlignment.Right;
            Background = UserMessageBackgroundBrush;
            Foreground = UserMessageForegroundBrush;
        }
        else
        {
            if (theme == ElementTheme.Default)
            {
                theme = Application.Current.RequestedTheme == ApplicationTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
            }

            Alignment = HorizontalAlignment.Left;
            Background = SenderMessageBackgroundBrushes[theme];
            Foreground = SenderMessageForegroundBrushes[theme];
        }
    }

    public string SentAtFormatted { get; set; } = null!;
    public HorizontalAlignment Alignment { get; set; }

    [ObservableProperty]
    private string text;

    [ObservableProperty]
    private Brush background;

    [ObservableProperty]
    private Brush foreground;

    [ObservableProperty]
    private bool edited;

    [ObservableProperty]
    private string editedAt;

    public void SetTheme(ElementTheme theme)
    {
        if (Message.UserIsSender)
        {
            Background = UserMessageBackgroundBrush;
            Foreground = UserMessageForegroundBrush;
        }
        else
        {
            Background = SenderMessageBackgroundBrushes[theme];
            Foreground = SenderMessageForegroundBrushes[theme];
        }
    }

    public void Update(string text, DateTime editedAtUtc, ElementTheme theme)
    {
        Text = text;
        EditedAt = FormatEditedAt(editedAtUtc);
        Edited = true;
        SetTheme(theme);
    }

    public void SetEditing()
    {
        Background = MessageEditingBackgroundBrush;
        Foreground = MessageEditingForegroundBrush;
    }

    private static string FormatEditedAt(DateTime editedAtUtc)
    {
        return $"Edited at {FormatDate(editedAtUtc)}";
    }

    private static string FormatDate(DateTime dateUtc)
    {
        return dateUtc.ToLocalTime().ToString(TimeFormat);
    }
}
