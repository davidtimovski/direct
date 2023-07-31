using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Direct.Desktop.Utilities;

namespace Direct.Desktop.ViewModels;

public partial class SetupViewModel : ObservableObject
{
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
