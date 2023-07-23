using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Direct.Desktop.Services;
using Direct.Desktop.Storage;
using Direct.Desktop.Utilities;
using Microsoft.UI.Xaml;

namespace Direct.Desktop.ViewModels;

public partial class SetupViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IChatService _chatService;

    public SetupViewModel(ISettingsService settingsService, IChatService chatService)
    {
        _settingsService = settingsService;
        _chatService = chatService;

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
        UserId = Guid.NewGuid().ToString();
    }

    public async Task ConnectAsync()
    {
        Connecting = true;

        var contactIds = await Repository.GetAllContactIdsAsync();

        await _chatService.ConnectAsync(_settingsService.UserId!.Value, contactIds);

        Connecting = false;
    }
}
