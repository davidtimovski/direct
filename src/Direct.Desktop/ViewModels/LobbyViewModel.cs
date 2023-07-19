using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Direct.Desktop.Services;
using Microsoft.UI.Xaml;

namespace Direct.Desktop.ViewModels;

public partial class LobbyViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IChatService _chatService;

    public LobbyViewModel(ISettingsService settingsService, IChatService chatService)
    {
        _settingsService = settingsService;
        _chatService = chatService;

        Nickname = _settingsService.Nickname;
        Theme = _settingsService.Theme;
    }

    [ObservableProperty]
    private ElementTheme theme;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConnectButtonEnabled))]
    private string nickname = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InputsEnabled))]
    [NotifyPropertyChangedFor(nameof(ConnectButtonEnabled))]
    private bool connecting;

    public bool InputsEnabled => !Connecting;

    public bool ConnectButtonEnabled => Nickname.Trim().Length > 1 && !Connecting;

    public async Task ConnectAsync()
    {
        Connecting = true;

        var nicknameTrimmed = Nickname.Trim();

        _settingsService.Nickname = nicknameTrimmed;
        _settingsService.Save();

        await _chatService.ConnectAsync(_settingsService.UserId, nicknameTrimmed);

        Connecting = false;
    }
}
