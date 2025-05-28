using System.ComponentModel;

namespace Heatwise.Interfaces;

public interface IPopupService : INotifyPropertyChanged
{
    bool IsPopupVisible { get; }
    IPopupViewModel? PopupContent { get; }

    void ShowPopup<TViewModel>() where TViewModel : IPopupViewModel, new();
    void ShowPopup(IPopupViewModel viewModel);
    void ClosePopup();
}
