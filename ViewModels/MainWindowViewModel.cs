using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sem2Proj.Models;

namespace Sem2Proj.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    // ViewModels
    public AssetManagerViewModel AssetManagerViewModel { get; }
    public OptimizerViewModel OptimizerViewModel { get; }
    public HomeViewModel HomeViewModel { get; }
    public SourceDataManagerViewModel SourceDataManagerViewModel { get; }

    public MainWindowViewModel(
        AssetManagerViewModel assetManagerViewModel,
        OptimizerViewModel optimizerViewModel,
        HomeViewModel homeViewModel,
        SourceDataManagerViewModel sourceDataManagerViewModel)
    {
        AssetManagerViewModel = assetManagerViewModel;
        OptimizerViewModel = optimizerViewModel;
        HomeViewModel = homeViewModel;
        SourceDataManagerViewModel = sourceDataManagerViewModel;
    }
}