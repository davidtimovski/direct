using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Direct.Desktop.Utilities;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Direct.Desktop.ViewModels;

public partial class MessageViewModel : ObservableObject
{
    private const double SideMargin = 50; 
    private const string TimeFormat = "HH:mm";

    private static readonly SolidColorBrush UserMessageBackgroundBrush = ResourceUtil.GetBrush("CyanBrush");
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

    public Guid Id { get; }
    public DateTime SentAt { get; }
    public bool IsRecipient { get; }

    public MessageViewModel(Guid id, string text, DateTime sentAt, DateTime? editedAt, bool isRecipient, ElementTheme theme, double messageFontSize)
    {
        Id = id;
        SentAt = sentAt;
        IsRecipient = isRecipient;

        Text = EmojiUtil.GenerateEmojis(text);
        SentAtFormatted = FormatDate(sentAt);
        Edited = editedAt.HasValue;
        EditedAt = editedAt.HasValue ? FormatEditedAt(editedAt.Value) : string.Empty;

        SetFontSize(messageFontSize);

        if (isRecipient)
        {
            if (theme == ElementTheme.Default)
            {
                theme = Application.Current.RequestedTheme == ApplicationTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
            }

            Alignment = HorizontalAlignment.Left;
            Margin = new Thickness(0, 0, SideMargin, 0);
            Background = SenderMessageBackgroundBrushes[theme];
            Foreground = SenderMessageForegroundBrushes[theme];
        }
        else
        {
            Alignment = HorizontalAlignment.Right;
            Margin = new Thickness(SideMargin, 0, 0, 0);
            Background = UserMessageBackgroundBrush;
            Foreground = UserMessageForegroundBrush;
        }
    }

    public string SentAtFormatted { get; }
    public HorizontalAlignment Alignment { get; }
    public Thickness Margin { get; }

    [ObservableProperty]
    private string text;

    [ObservableProperty]
    private double messageFontSize;

    [ObservableProperty]
    private double metadataFontSize;

    [ObservableProperty]
    private double editedMessageIconFontSize;

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
        if (IsRecipient)
        {
            Background = SenderMessageBackgroundBrushes[theme];
            Foreground = SenderMessageForegroundBrushes[theme];
        }
        else
        {
            Background = UserMessageBackgroundBrush;
            Foreground = UserMessageForegroundBrush;
        }
    }

    public void SetFontSize(double messageFontSize)
    {
        MessageFontSize = messageFontSize;
        MetadataFontSize = messageFontSize - 1;
        EditedMessageIconFontSize = messageFontSize - 3;
    }

    public void Update(string text, DateTime editedAt, ElementTheme theme)
    {
        Text = text;
        EditedAt = FormatEditedAt(editedAt);
        Edited = true;
        SetTheme(theme);
    }

    public void SetEditing()
    {
        Background = MessageEditingBackgroundBrush;
        Foreground = MessageEditingForegroundBrush;
    }

    private static string FormatEditedAt(DateTime editedAt)
    {
        return $"Edited at {FormatDate(editedAt)}";
    }

    private static string FormatDate(DateTime date)
    {
        return date.ToString(TimeFormat);
    }
}
