using Avalonia.Controls;
using Avalonia.Input;
using Sem2Proj.ViewModels;
using Sem2Proj.Interfaces;


namespace Sem2Proj.Views;

public partial class LoginView : Window
{
    public LoginView(
        AssetManagerViewModel assetManagerViewModel,
        OptimizerViewModel optimizerViewModel,
        SourceDataManagerViewModel sourceDataManagerViewModel,
        IPopupService popupService)
    {
        InitializeComponent();
        DataContext = new LoginViewModel(this, assetManagerViewModel, optimizerViewModel, sourceDataManagerViewModel, popupService);
    }

     public LoginView() // <- you still keep this empty constructor too
    {
        InitializeComponent();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is LoginViewModel viewModel)
            {
                // Trigger the AttemptLoginCommand
                if (viewModel.AttemptLoginCommand.CanExecute(null))
                {
                    viewModel.AttemptLoginCommand.Execute(null);
                }
            }
        }
    }
}
