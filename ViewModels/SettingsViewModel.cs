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
    private bool isDarkMode;

    public ICommand? CloseCommand { get; private set; }

    public SettingsViewModel()
    {
        // Load the theme setting from the database
        var theme = _dataManager.GetSetting("Theme");
        IsDarkMode = theme == "Dark"; // Default to dark mode if no setting exists
    }

    partial void OnIsDarkModeChanged(bool value)
    {
        // Save the theme setting to the database
        _dataManager.SaveSetting("Theme", value ? "Dark" : "Light");
          (Application.Current as App)?.UpdateTheme(value);
    }

    public void SetCloseAction(Action closeCallback)
    {
        CloseCommand = new RelayCommand(() => closeCallback());
    }
}