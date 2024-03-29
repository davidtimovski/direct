﻿using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Direct.Desktop.Services;
using Direct.Desktop.Storage;
using Direct.Desktop.Utilities;
using Microsoft.UI.Xaml;

namespace Direct.Desktop.ViewModels;

public partial class EditContactViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IEventService _eventService;
    private readonly string _originalNickname;

    public EditContactViewModel(ISettingsService settingsService, IEventService eventService, string userId, string nickname)
    {
        _settingsService = settingsService;
        _settingsService.Changed += SettingsChanged;

        _eventService = eventService;

        _originalNickname = nickname;

        Theme = _settingsService.Theme;
        UserId = userId;
        Nickname = nickname;
    }

    private void SettingsChanged(object? _, SettingsChangedEventArgs e)
    {
        if (e.ChangedSetting == Setting.Theme)
        {
            Theme = e.Theme;
        }
    }

    [ObservableProperty]
    private ElementTheme theme;

    public string UserId { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SaveButtonEnabled))]
    private string nickname = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NicknameTextBoxEnabled))]
    [NotifyPropertyChangedFor(nameof(SaveButtonEnabled))]
    private bool saving;

    public bool NicknameTextBoxEnabled => !Saving;

    public bool SaveButtonEnabled =>
        ValidationUtil.NicknameIsValid(Nickname)
        && !string.Equals(_originalNickname, Nickname.Trim(), StringComparison.InvariantCulture)
        && !Saving;

    public async Task SaveContactAsync()
    {
        Saving = true;

        var id = new Guid(UserId);
        var nickname = Nickname.Trim();

        await Repository.UpdateContactNicknameAsync(id, nickname);
        _eventService.RaiseContactEdited(id, nickname);
    }
}
