using CommunityToolkit.Mvvm.ComponentModel;
using Sem2Proj.Interfaces;

namespace Sem2Proj.Services;

public partial class PopupService : ObservableObject, IPopupService
{
    [ObservableProperty]
    private bool _isPopupVisible;

    [ObservableProperty]
    private object? _popupContent;

    public void ShowPopup<TViewModel>() where TViewModel : IPopupViewModel, new()
    {
        var viewModel = new TViewModel();
        ShowPopup(viewModel);
    }

    public void ShowPopup(IPopupViewModel viewModel)
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