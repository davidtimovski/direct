using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Direct.Services;
using Direct.Utilities;
using Microsoft.UI.Xaml;

namespace Direct.ViewModels;

public partial class LobbyViewModel : ObservableObject
{
    private readonly IStorageService _storageService;
    private readonly IChatService _chatService;
    private readonly string _passwordHash;

    public LobbyViewModel(IStorageService storageService, IChatService chatService)
    {
        _storageService = storageService;
        _chatService = chatService;

        _passwordHash = _storageService.AppData.PasswordHash;
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

    public bool PasswordBoxVisible => _passwordHash == string.Empty;

    public bool ConnectButtonEnabled => Nickname.Trim().Length > 1 && !Connecting;

    public async Task ConnectAsync()
    {
        Connecting = true;

        var passwordHashed = _passwordHash == string.Empty ? CryptographyUtil.Hash(Password) : _passwordHash;
        var nicknameTrimmed = Nickname.Trim();

        _storageService.AppData.PasswordHash = passwordHashed;
        _storageService.AppData.Nickname = nicknameTrimmed;
        _storageService.Save();

        await _chatService.ConnectAsync(passwordHashed, nicknameTrimmed);
    }
}
