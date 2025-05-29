using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heatwise.Interfaces;

namespace Heatwise.ViewModels;

public partial class ToolTipViewModel : ViewModelBase, IPopupViewModel
{
    public ICommand? CloseCommand { get; private set; }
    public bool IsDraggable => true; // Set to true if the popup should be draggable
    public bool ShowBackdrop => false; // Set to true if the popup should show a backdrop
    public PopupStartupLocation StartupLocation => IsDraggable ? PopupStartupLocation.Custom : PopupStartupLocation.Center;
    public ToolTipViewModel()
    {
    }
    public void SetCloseAction(Action closeCallback)
    {
        CloseCommand = new RelayCommand(() => closeCallback());
    }

    [ObservableProperty]
    private string tooltipText = string.Empty;

}