using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Direct.Desktop.Services;
using Direct.Desktop.Storage;
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

        var contactIds = await Repository.GetAllContactIdsAsync();

        await _chatService.ConnectAsync(_settingsService.UserId, contactIds);

        Connecting = false;
    }
}
