using System.ComponentModel;
using Avalonia;

namespace Heatwise.Interfaces;

public interface IPopupService : INotifyPropertyChanged
{
    bool IsPopupVisible { get; }
    IPopupViewModel? PopupContent { get; }
    Thickness PopupMargin { get; } 
    public bool IsDraggable => true; // Set to true if the popup should be draggable
    public bool ShowBackdrop => false; // Set to true if the popup should show a backdrop

     public PopupStartupLocation StartupLocation => IsDraggable ? PopupStartupLocation.Custom : PopupStartupLocation.Center;

    void SetPopupMargin(Thickness margin);
    void ShowPopup<TViewModel>() where TViewModel : IPopupViewModel, new();
    void ShowPopup(IPopupViewModel viewModel);
    void ClosePopup();
}