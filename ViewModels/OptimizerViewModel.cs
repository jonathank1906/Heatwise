using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScottPlot;
using ScottPlot.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sem2Proj.ViewModels;

public partial class OptimizerViewModel : ViewModelBase
{
    private const int OpenWidth = 275;
    private const int ClosedWidth = 0;

    [ObservableProperty]
    private double _paneWidth = OpenWidth;

    [ObservableProperty]
    private bool _isPaneOpen = true;

    [ObservableProperty]
    private bool _isOpening;

    [RelayCommand]
    private async Task TriggerPane()
    {
        IsOpening = !IsPaneOpen;
        IsPaneOpen = !IsPaneOpen;
        PaneWidth = IsPaneOpen ? OpenWidth : ClosedWidth;
    }



    public OptimizerViewModel()
    {
        
    }
}