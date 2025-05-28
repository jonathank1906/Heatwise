using CommunityToolkit.Mvvm.ComponentModel;
using Heatwise.Interfaces;

namespace Heatwise.Services;

public partial class PopupToolTipService : ObservableObject, IPopupToolTipService
{
    [ObservableProperty]
    private bool _isPopupVisible;

    [ObservableProperty]
    private IPopupToolTipViewModel? _popupContent; // Change type from object? to IPopupViewModel?

    public void ShowPopup<TViewModel>() where TViewModel : IPopupToolTipViewModel, new()
    {
        var viewModel = new TViewModel();
        ShowPopup(viewModel);
    }

    public void ShowPopup(IPopupToolTipViewModel viewModel)
    {
        viewModel.SetCloseAction(ClosePopup);
        PopupContent = viewModel;
        IsPopupVisible = true;
    }

    public void ClosePopup()
    {
        IsPopupVisible = false;
        PopupContent = null;
    }
}