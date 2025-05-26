using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace Heatwise.ViewModels;

public partial class SourceDataManagerViewModel : ViewModelBase
{
    private const int OpenWidth = 350;
    private const int ClosedWidth = 0;

    [ObservableProperty]
    private double _paneWidth = OpenWidth;

    [ObservableProperty]
    private bool _isPaneOpen = true;

    [ObservableProperty]
    private bool _isOpening;

    [RelayCommand]
    private void TriggerPane()
    {
        IsOpening = !IsPaneOpen;
        IsPaneOpen = !IsPaneOpen;
        PaneWidth = IsPaneOpen ? OpenWidth : ClosedWidth;
    }



    public SourceDataManagerViewModel()
    {
        
    }
}