using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Heatwise.Interfaces;

namespace Heatwise.ViewModels;

public partial class AMHelpViewModel : ViewModelBase, IPopupViewModel
{
    public ICommand? CloseCommand { get; private set; }
    public bool IsDraggable => true; 
    public bool ShowBackdrop => true; 
    public PopupStartupLocation StartupLocation => IsDraggable ? PopupStartupLocation.Center : PopupStartupLocation.Custom;
    public AMHelpViewModel()
    {
    }
    public void SetCloseAction(Action closeCallback)
    {
        CloseCommand = new RelayCommand(() => closeCallback());
    }
}