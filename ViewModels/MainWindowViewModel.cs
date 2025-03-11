using System;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using Sem2Proj.Models;

namespace Sem2Proj.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    // ViewsModels
    public ScenarioManagerViewModel ScenarioManagerViewModel { get; }
    public AssetManagerViewModel AssetManagerViewModel { get; }
    public OptimizerViewModel OptimizerViewModel { get; }
    public HomeViewModel HomeViewModel { get; }

    public MainWindowViewModel()
    {
        ScenarioManagerViewModel = new ScenarioManagerViewModel();
        AssetManagerViewModel = new AssetManagerViewModel();
        OptimizerViewModel = new OptimizerViewModel();
        HomeViewModel = new HomeViewModel();
    }
}