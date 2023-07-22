using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Direct.Desktop.Services;
using Direct.Desktop.Storage;
using Microsoft.UI.Xaml;

namespace Direct.Desktop.ViewModels;

public partial class NewContactViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IChatService _chatService;
    private readonly IEventService _eventService;

    public NewContactViewModel(ISettingsService settingsService, IChatService chatService, IEventService eventService)
    {
        _settingsService = settingsService;
        _settingsService.ThemeChanged += ThemeChanged;

        _chatService = chatService;
        _eventService = eventService;

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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AddButtonEnabled))]
    private string nickname = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AddButtonEnabled))]
    private bool adding;

    public bool AddButtonEnabled => UserId.Length == 36
        && Guid.TryParse(UserId, out Guid _)
        && Nickname.Trim().Length > 0
        && !Adding;

    public async Task AddContactAsync()
    {
        Adding = true;

        var contact = new Contact { Id = new Guid(UserId), Nickname = Nickname.Trim(), AddedOn = DateTime.Now };
        await Repository.CreateContactAsync(contact);
        _eventService.RaiseContactAdded(contact.Id, contact.Nickname);

        await _chatService.AddContactAsync(contact.Id);
    }
}
