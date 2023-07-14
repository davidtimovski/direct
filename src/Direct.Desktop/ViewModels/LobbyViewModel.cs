using System.Threading.Tasks;
using Direct.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;

namespace Direct.ViewModels;

public partial class LobbyViewModel : ObservableObject
{
    private readonly IStorageService _storageService;
    private readonly IChatService _chatService;

    public LobbyViewModel(IStorageService storageService, IChatService chatService)
    {
        _storageService = storageService;
        _chatService = chatService;

        Password = _storageService.AppData.PasswordHash;
        Nickname = _storageService.AppData.Nickname;
        Theme = _storageService.AppData.Theme;
    }

    [ObservableProperty]
    private ElementTheme theme;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConnectButtonEnabled))]
    private string nickname = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConnectButtonEnabled))]
    private string password = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InputsEnabled))]
    [NotifyPropertyChangedFor(nameof(ConnectButtonEnabled))]
    private bool connecting;

    public bool InputsEnabled => !Connecting;

    public bool ConnectButtonEnabled => Nickname.Trim().Length > 1 && !Connecting;

    public async Task ConnectAsync()
    {
        Connecting = true;

        await _chatService.ConnectAsync(Password, Nickname.Trim());
    }
}
