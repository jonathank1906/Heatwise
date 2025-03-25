using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace Sem2Proj.ViewModels;

public partial class OptimizerViewModel : ViewModelBase
{
    private const int OpenWidth = 275;
    private const int ClosedWidth = 0;
    
    [ObservableProperty]
    private double _paneWidth = OpenWidth;
    
    [ObservableProperty]
    private string _toggleSymbol = "←";
    
    [ObservableProperty]
    private bool _isPaneOpen = true;

    [RelayCommand]
    private async Task TriggerPane()
    {
        IsPaneOpen = !IsPaneOpen;
        ToggleSymbol = IsPaneOpen ? "←" : "≡";
        
        // Animate the width change
        var targetWidth = IsPaneOpen ? OpenWidth : ClosedWidth;
        var step = (targetWidth - PaneWidth) / 10;
        
        for (int i = 0; i < 10; i++)
        {
            PaneWidth += step;
            await Task.Delay(10);
        }
        PaneWidth = targetWidth; // Ensure exact final value
    }
}