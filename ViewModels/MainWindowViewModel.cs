using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace Sem2Proj.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private int selectedTabIndex;
    
    /*
    [ObservableProperty]
    private string imageSource = "avares://Sem2Proj/Assets/white-danfoss-logo.png";

    */
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
    /*
    partial void OnSelectedTabIndexChanged(int oldValue, int newValue)
    {
        if (newValue == 3)
        {
            ImageSource = "avares://Sem2Proj/Assets/white-danfoss-logo.png";
            Console.WriteLine($"Image source changed to: {ImageSource}");
        }
        else
        {
            ImageSource = "avares://Sem2Proj/Assets/black-danfoss-logo.png";
            Console.WriteLine($"Image source changed to: {ImageSource}");
        }
    }
    */
}