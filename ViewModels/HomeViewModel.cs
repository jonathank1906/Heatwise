using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heatwise.Interfaces;

namespace Heatwise.ViewModels;

public partial class HomeViewModel : ViewModelBase, IPopupViewModel
{
    public ICommand? CloseCommand { get; private set; }
    public bool IsDraggable => true; // Set to true if the popup should be draggable
    public bool ShowBackdrop => true; // Set to true if the popup should show a backdrop
    public PopupStartupLocation StartupLocation => IsDraggable ? PopupStartupLocation.Center : PopupStartupLocation.Custom;
    public HomeViewModel()
    {
    }
    public void SetCloseAction(Action closeCallback)
    {
        CloseCommand = new RelayCommand(() => closeCallback());
    }
}