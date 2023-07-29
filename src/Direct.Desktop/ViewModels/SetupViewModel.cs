using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Direct.Desktop.Services;
using Direct.Desktop.Utilities;
using Microsoft.UI.Xaml;

namespace Direct.Desktop.ViewModels;

public partial class SetupViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    public SetupViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;

        Theme = _settingsService.Theme;
    }

    [ObservableProperty]
    private ElementTheme theme;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UserIdTextBoxEnabled))]
    [NotifyPropertyChangedFor(nameof(LetsGoButtonEnabled))]
    private string userId = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UserIdTextBoxEnabled))]
    [NotifyPropertyChangedFor(nameof(GenerateButtonEnabled))]
    [NotifyPropertyChangedFor(nameof(LetsGoButtonEnabled))]
    private bool connecting;

    public bool UserIdTextBoxEnabled => !Connecting;
    public bool GenerateButtonEnabled => !Connecting;
    public bool LetsGoButtonEnabled => ValidationUtil.UserIdIsValid(UserId) && !Connecting;

    public void GenerateUserID()
    {
        UserId = Guid.NewGuid().ToString("N");
    }
}
