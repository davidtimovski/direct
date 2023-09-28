using CommunityToolkit.Mvvm.ComponentModel;
using Direct.Desktop.Services;

namespace Direct.Desktop.ViewModels;

public partial class FeaturesSettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    public FeaturesSettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        EmojiPickerEnabled = _settingsService.EmojiPickerEnabled;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EmojiPickerCheckBoxTooltip))]
    private bool emojiPickerEnabled;

    public string EmojiPickerCheckBoxTooltip => EmojiPickerEnabled ? "Disable" : "Enable";

    partial void OnEmojiPickerEnabledChanged(bool value)
    {
        _settingsService.EmojiPickerEnabled = value;
    }
}
