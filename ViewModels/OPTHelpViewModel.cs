using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Heatwise.Interfaces;

namespace Heatwise.ViewModels;

public partial class OPTHelpViewModel : ViewModelBase, IPopupViewModel
{
    public ICommand? CloseCommand { get; private set; }
    public bool IsDraggable => false; 
    public bool ShowBackdrop => true;
    public PopupStartupLocation StartupLocation => IsDraggable ? PopupStartupLocation.Custom : PopupStartupLocation.Center;
    public OPTHelpViewModel()
    {
    }
    public void SetCloseAction(Action closeCallback)
    {
        CloseCommand = new RelayCommand(() => closeCallback());
    }
}