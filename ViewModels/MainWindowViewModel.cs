using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sem2Proj.Models;
using Sem2Proj.Interfaces;

namespace Sem2Proj.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty] private bool isPopupVisible = false;
    [ObservableProperty] private object? popupContent = null;

    // ViewModels
    public AssetManagerViewModel AssetManagerViewModel { get; }
    public OptimizerViewModel OptimizerViewModel { get; }
    public SourceDataManagerViewModel SourceDataManagerViewModel { get; }

    public MainWindowViewModel(
        AssetManagerViewModel assetManagerViewModel,
        OptimizerViewModel optimizerViewModel,
        SourceDataManagerViewModel sourceDataManagerViewModel)
    {
        AssetManagerViewModel = assetManagerViewModel;
        OptimizerViewModel = optimizerViewModel;
        SourceDataManagerViewModel = sourceDataManagerViewModel;
        
        ShowHome();
    }

    // A quite fancy method to show a popup
    // It uses generics to allow any ViewModel to be shown in the popup
    // It also sets the close action for the ViewModel
    // Could have used a more generic approach, but this is cooler
    private void ShowPopup<TViewModel>() where TViewModel : IPopupViewModel, new()
    {
        var viewModel = new TViewModel();
        viewModel.SetCloseAction(ClosePopup);
        PopupContent = viewModel;
        IsPopupVisible = true;
    }
    public void ClosePopup()
    {
        // No animations yet
        IsPopupVisible = false;
        PopupContent = null;
    }

    [RelayCommand] public void ShowHome()
    {
        ShowPopup<HomeViewModel>();
    }
    [RelayCommand] public void ShowSettings()
    {
        ShowPopup<SettingsViewModel>();
    }
}