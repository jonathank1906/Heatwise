using Avalonia.Controls;
using Avalonia.Input;
using Sem2Proj.ViewModels;
using Sem2Proj.Interfaces;
using System;


namespace Sem2Proj.Views;

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
