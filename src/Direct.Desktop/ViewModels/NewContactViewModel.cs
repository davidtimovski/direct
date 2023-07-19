using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Direct.Desktop.Services;
using Microsoft.UI.Xaml;

namespace Direct.Desktop.ViewModels;

public partial class NewContactViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IChatService _chatService;

    public NewContactViewModel(ISettingsService settingsService, IChatService chatService)
    {
        _settingsService = settingsService;
        _settingsService.ThemeChanged += ThemeChanged;

        _chatService = chatService;

        Theme = _settingsService.Theme;
    }

    private void ThemeChanged(object? _, ThemeChangedEventArgs e)
    {
        Theme = e.Theme;
    }

    [ObservableProperty]
    private ElementTheme theme;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AddButtonEnabled))]
    private string userId = string.Empty;

    public bool AddButtonEnabled => UserId.Length == 36 && Guid.TryParse(UserId, out Guid _);

    public void AddContact()
    {

    }
}
