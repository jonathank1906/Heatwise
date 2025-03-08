using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;


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
    }
}