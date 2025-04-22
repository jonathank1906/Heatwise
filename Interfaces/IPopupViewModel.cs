using System.ComponentModel;

namespace Sem2Proj.Interfaces;

public interface IPopupService : INotifyPropertyChanged
{
    bool IsPopupVisible { get; }
    object? PopupContent { get; }

    void ShowPopup<TViewModel>() where TViewModel : IPopupViewModel, new();
    void ShowPopup(IPopupViewModel viewModel);
    void ClosePopup();
}
