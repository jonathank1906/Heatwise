using Avalonia.Controls;

using Sem2Proj.ViewModels;
namespace Sem2Proj.Views;

public partial class DialogWindow : Window
{
    public DialogWindow()
    {
        InitializeComponent();
    }

    private void Confirm(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(true); // Return 'true' when OK is clicked
    }

    private void Cancel(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(false); // Return 'false' when Cancel is clicked
    }
}

