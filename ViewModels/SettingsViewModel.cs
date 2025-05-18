using System;
using System.Diagnostics;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sem2Proj.Interfaces;
using Sem2Proj.Models;
using Avalonia;

namespace Sem2Proj.ViewModels;

public partial class SettingsViewModel : ViewModelBase, IPopupViewModel
{
    private SourceDataManager _dataManager = new SourceDataManager(); // Use a default instance

    [ObservableProperty]
    private bool light_Mode_On_Toggle;

    [ObservableProperty]
    private bool home_Screen_On_Startup_Toggle;

    [ObservableProperty]
    private bool developer_Mode_On_Toggle;
    public ICommand? CloseCommand { get; private set; }
    public ICommand RestoreDefaultsCommand { get; private set; }


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
    }
    partial void OnDeveloper_Mode_On_ToggleChanged(bool value)
    {
        _dataManager.SaveSetting("Developer_Mode", value ? "On" : "Off");
    }

    public void RestoreDefaults()
    {
        // Set all properties to default values
        Light_Mode_On_Toggle = false; // Dark theme
        Home_Screen_On_Startup_Toggle = true; // Off
        Developer_Mode_On_Toggle = false; // Off
        
        // The property change handlers will save the settings
    }
    

    public void SetCloseAction(Action closeCallback)
    {
        CloseCommand = new RelayCommand(() => closeCallback());
    }
}