using System.ComponentModel;

namespace Heatwise.Interfaces;

public interface IPopupToolTipService : INotifyPropertyChanged
{
    bool IsPopupVisible { get; }
    IPopupToolTipViewModel? PopupContent { get; }

    void ShowPopup<TViewModel>() where TViewModel : IPopupToolTipViewModel, new();
    void ShowPopup(IPopupToolTipViewModel viewModel);
    void ClosePopup();
}
