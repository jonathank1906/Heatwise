using System;
using System.Diagnostics;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heatwise.Interfaces;
using Heatwise.Models;
using Avalonia;

namespace Heatwise.ViewModels;

public partial class SettingsViewModel : ViewModelBase, IPopupViewModel
{
    private SourceDataManager _dataManager = new SourceDataManager(); // Use a default instance

    [ObservableProperty]
    private bool light_Mode_On_Toggle;

    [ObservableProperty]
    private bool home_Screen_On_Startup_Toggle;
    [ObservableProperty]
    private bool is_Home_Screen_On_Startup_Toggle_Enabled = true;

    [ObservableProperty]
    private bool developer_Mode_On_Toggle;
    public ICommand? CloseCommand { get; private set; }
    public ICommand RestoreDefaultsCommand { get; private set; }
       public bool IsDraggable => true; // Set to true if the popup should be draggable
    public bool ShowBackdrop => false; // Set to true if the popup should show a backdrop
    public PopupStartupLocation StartupLocation => IsDraggable ? PopupStartupLocation.Custom : PopupStartupLocation.Center;

    public SettingsViewModel()
    {
        // Load settings
        var theme = _dataManager.GetSetting("Theme");
        var homeToggle = _dataManager.GetSetting("Home_Screen_On_Startup");
        var developerModeToggle = _dataManager.GetSetting("Developer_Mode");

        // Set settings
        Light_Mode_On_Toggle = theme != "Dark";
        Home_Screen_On_Startup_Toggle = homeToggle != "Off";
        Developer_Mode_On_Toggle = developerModeToggle != "Off";

        RestoreDefaultsCommand = new RelayCommand(RestoreDefaults);
    }

    partial void OnLight_Mode_On_ToggleChanged(bool value)
    {
        _dataManager.SaveSetting("Theme", value ? "Light" : "Dark");
        (Application.Current as App)?.UpdateTheme(value);
    }
    partial void OnHome_Screen_On_Startup_ToggleChanged(bool value)
    {
        _dataManager.SaveSetting("Home_Screen_On_Startup", value ? "On" : "Off");
        if (Developer_Mode_On_Toggle && Home_Screen_On_Startup_Toggle)
            Home_Screen_On_Startup_Toggle = false;
    }
    partial void OnDeveloper_Mode_On_ToggleChanged(bool value)
    {
        _dataManager.SaveSetting("Developer_Mode", value ? "On" : "Off");
        if (Developer_Mode_On_Toggle && Home_Screen_On_Startup_Toggle)
        {
            Is_Home_Screen_On_Startup_Toggle_Enabled = false;
            Home_Screen_On_Startup_Toggle = false;
        }
        if (Developer_Mode_On_Toggle)
            Is_Home_Screen_On_Startup_Toggle_Enabled = false;
        else
            Is_Home_Screen_On_Startup_Toggle_Enabled = true;
    }

    public void RestoreDefaults()
    {
        Light_Mode_On_Toggle = true; // Dark theme
        Developer_Mode_On_Toggle = false; // Off
        Home_Screen_On_Startup_Toggle = true; // On
    }

    public void SetCloseAction(Action closeCallback)
    {
        CloseCommand = new RelayCommand(() => closeCallback());
    }
}