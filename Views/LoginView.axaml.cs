using Avalonia.Controls;
using Avalonia.Input;
using Heatwise.ViewModels;
using Heatwise.Interfaces;
using System;


namespace Heatwise.Views;

public partial class LoginView : Window
{

    public LoginView()
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
