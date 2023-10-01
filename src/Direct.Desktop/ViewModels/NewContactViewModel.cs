using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Direct.Desktop.Services;
using Direct.Desktop.Storage;
using Direct.Desktop.Storage.Entities;
using Direct.Desktop.Utilities;
using Microsoft.UI.Xaml;

namespace Direct.Desktop.ViewModels;

public partial class NewContactViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IContactProxy _contactProxy;
    private readonly IEventService _eventService;

    public NewContactViewModel(ISettingsService settingsService, IContactProxy contactProxy, IEventService eventService)
    {
        _settingsService = settingsService;
        _settingsService.Changed += SettingsChanged;

        _contactProxy = contactProxy;
        _eventService = eventService;

        Theme = _settingsService.Theme;
    }

    private void SettingsChanged(object? _, SettingsChangedEventArgs e)
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
    [NotifyPropertyChangedFor(nameof(TextBoxesEnabled))]
    [NotifyPropertyChangedFor(nameof(AddButtonEnabled))]
    private bool adding;

    public bool TextBoxesEnabled => !Adding;

    public bool AddButtonEnabled =>
        ValidationUtil.UserIdIsValid(UserId)
        && Guid.ParseExact(UserId, "N") != _settingsService.UserId
        && ValidationUtil.NicknameIsValid(Nickname)
        && !Adding;

    public async Task AddContactAsync()
    {
        Adding = true;

        var contact = new Contact
        (
            Guid.ParseExact(UserId, "N"),
            Nickname.Trim(),
            DateTime.Now
        );
        await Repository.CreateContactAsync(contact);
        _eventService.RaiseContactAdded(contact.Id, contact.Nickname);

        await _contactProxy.AddContactAsync(contact.Id);
    }
}
