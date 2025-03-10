using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace Sem2Proj.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
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

        Console.WriteLine(OperatingSystem.IsMacOS());
        Console.WriteLine(OperatingSystem.IsWindows());
        Console.WriteLine(OperatingSystem.IsLinux());
    }
}