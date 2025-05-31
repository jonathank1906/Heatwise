using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sem2Proj.Models;
using Sem2Proj.Interfaces;

namespace Sem2Proj.ViewModels;

public partial class MachineViewModel : ViewModelBase
{
    private readonly AssetManager _assetManager;
    private readonly IPopupService _popupService;

    [ObservableProperty]
    private ViewState _currentViewState = ViewState.ScenarioSelection;

    public MachineViewModel(AssetManager assetManager, IPopupService popupService)
    {
        _assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));
        _popupService = popupService ?? throw new ArgumentNullException(nameof(popupService));
    }

    [RelayCommand]
    public void ShowConfiguration()
    {
        CurrentViewState = ViewState.Configure;
    }

    [RelayCommand]
    public void CancelConfiguration()
    {
        CurrentViewState = ViewState.ScenarioSelection;
    }
} 