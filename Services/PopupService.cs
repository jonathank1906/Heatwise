using CommunityToolkit.Mvvm.ComponentModel;
using Heatwise.Interfaces;
using Avalonia;

namespace Heatwise.Services;

public partial class PopupService : ObservableObject, IPopupService
{
    [ObservableProperty]
    private bool _isPopupVisible;

    [ObservableProperty]
    private IPopupViewModel? _popupContent;

    [ObservableProperty]
    private Thickness _popupMargin = new(0);

    public void ShowPopup<TViewModel>() where TViewModel : IPopupViewModel, new()
    {
        var viewModel = new TViewModel();
        ShowPopup(viewModel);
    }

    public void ShowPopup(IPopupViewModel viewModel)
    {
        viewModel.SetCloseAction(ClosePopup);

        // Set margin based on StartupLocation
        PopupMargin = GetMarginForStartupLocation(viewModel.StartupLocation);

        PopupContent = viewModel;
        IsPopupVisible = true;
    }

    public void ClosePopup()
    {
        IsPopupVisible = false;
        PopupContent = null;
    }

    private Thickness GetMarginForStartupLocation(PopupStartupLocation location)
    {
        switch (location)
        {
            case PopupStartupLocation.Center:
                return new Thickness(0);
            case PopupStartupLocation.Custom:
                return new Thickness(200, 200, 0, 0); // Example custom
            default:
                return new Thickness(0);
        }
    }

    public void SetPopupMargin(Thickness margin)
    {
        PopupMargin = margin;
    }

}