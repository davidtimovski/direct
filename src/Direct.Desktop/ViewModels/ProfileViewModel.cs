using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Direct.Desktop.Models;
using Direct.Desktop.Services;
using Direct.Desktop.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace Direct.Desktop.ViewModels;

public partial class ProfileViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IContactProxy _contactProxy;

    public ProfileViewModel(ISettingsService settingsService, IContactProxy contactProxy)
    {
        _settingsService = settingsService;
        _settingsService.Changed += SettingsChanged;

        _contactProxy = contactProxy;

        Theme = _settingsService.Theme;
        UserId = _settingsService.UserId!.Value.ToString("N");

        ProfileImageOptions = ProfileImageUtil.Lookup.Select(x => new ProfileImageOption
        {
            Name = x.Key,
            Source = x.Value
        }).ToList();
        SelectedProfileImage = ProfileImageOptions.First(x => x.Name == _settingsService.ProfileImage);
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

    [ObservableProperty]
    private string userId = string.Empty;

    public IReadOnlyList<ProfileImageOption> ProfileImageOptions { get; }

    [ObservableProperty]
    private ProfileImageOption selectedProfileImage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SaveButtonEnabled))]
    private bool saving;

    public bool SaveButtonEnabled => true && !Saving;

    public void CopyID()
    {
        var package = new DataPackage();
        package.SetText(UserId);
        Clipboard.SetContent(package);
    }

    public async void ProfileImageChangedAsync(object _, SelectionChangedEventArgs e)
    {
        _settingsService.ProfileImage = SelectedProfileImage.Name;
        await _contactProxy.UpdateProfileImageAsync(SelectedProfileImage.Name);
    }
}
