using System;
using Direct.Desktop.Services;
using Direct.Desktop.Utilities;
using Direct.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace Direct.Desktop;

public sealed partial class SetupWindow : Window
{
    private readonly ISettingsService _settingsService;
    private readonly IServiceProvider _serviceProvider;

    public SetupViewModel ViewModel { get; }

    public SetupWindow(ISettingsService settingsService, IServiceProvider serviceProvider, SetupViewModel viewModel)
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        _settingsService = settingsService;
        _serviceProvider = serviceProvider;
        ViewModel = viewModel;
    }

    public void Start()
    {
        _settingsService.UserId = Guid.ParseExact(ViewModel.UserId, "N");
        _settingsService.Save();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        WindowingUtil.Resize(mainWindow, new SizeInt32(_settingsService.WindowWidth, _settingsService.WindowHeight));
        mainWindow.Activate();

        Close();
    }
}
