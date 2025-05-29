using System.ComponentModel;
using Avalonia;

namespace Heatwise.Interfaces;

public interface IPopupService : INotifyPropertyChanged
{
    bool IsPopupVisible { get; }
    IPopupViewModel? PopupContent { get; }
    Thickness PopupMargin { get; }
    public bool IsDraggable => true;
    public bool ShowBackdrop => false;

    public PopupStartupLocation StartupLocation => IsDraggable ? PopupStartupLocation.Custom : PopupStartupLocation.Center;

    void SetPopupMargin(Thickness margin);
    void ShowPopup<TViewModel>() where TViewModel : IPopupViewModel, new();
    void ShowPopup(IPopupViewModel viewModel);
    void ClosePopup();
}