using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Direct.Shared.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Direct.ViewModels;

public partial class MessageViewModel : ObservableObject
{
    private static readonly SolidColorBrush UserMessageBackgroundBrush = new(Color.FromArgb(255, 25, 65, 150));
    private static readonly SolidColorBrush UserMessageForegroundBrush = new(Colors.White);
    private static readonly Dictionary<ElementTheme, SolidColorBrush> SenderMessageBackgroundBrushes = new()
    {
        { ElementTheme.Light, new SolidColorBrush(Color.FromArgb(255, 230, 230, 230)) },
        { ElementTheme.Dark, new SolidColorBrush(Color.FromArgb(255, 50, 50, 50)) },
    };
    private static readonly Dictionary<ElementTheme, SolidColorBrush> SenderMessageForegroundBrushes = new()
    {
        { ElementTheme.Light, new SolidColorBrush(Colors.Black) },
        { ElementTheme.Dark, new SolidColorBrush(Colors.White) },
    };
    private static readonly SolidColorBrush UserMessageEditingBackgroundBrush = new(Color.FromArgb(255, 150, 65, 25));
    private static readonly SolidColorBrush UserMessageEditingForegroundBrush = new(Colors.White);

    public MessageDto Message { get; }

    public MessageViewModel(MessageDto message, ElementTheme theme)
    {
        Message = message;

        Text = message.Text;
        SentAtFormatted = message.SentAtUtc.ToLocalTime().ToString("HH:mm:ss");
        Edited = message.EditedAtUtc.HasValue;
        EditedAt = message.EditedAtUtc.HasValue ? $"Edited at {message.EditedAtUtc.Value.ToLocalTime():HH:mm:ss}" : string.Empty;

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
        EditedAt = editedAtUtc.ToLocalTime().ToString("HH:mm:ss");
        Edited = true;
        SetTheme(theme);
    }

    public void SetEditing()
    {
        Background = UserMessageEditingBackgroundBrush;
        Foreground = UserMessageEditingForegroundBrush;
    }
}
