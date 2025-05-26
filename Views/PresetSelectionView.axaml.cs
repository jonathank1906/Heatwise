using Avalonia.Controls;
using Avalonia.Input;
using Heatwise.ViewModels;
using Avalonia.Interactivity;
using System.Diagnostics;

namespace Heatwise.Views;

public partial class PresetSelectionView : UserControl
{
    public PresetSelectionView()
    {
        InitializeComponent();
    }

    private void OnRenameKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox textBox)
        {
            // Prevent the Enter key from being handled further
            e.Handled = true;

            // Directly execute the finish command
            if (DataContext is AssetManagerViewModel viewModel)
            {
                viewModel.FinishRenamingCommand.Execute(textBox.DataContext);
            }
        }
    }

    private void OnRenameLostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AssetManagerViewModel viewModel && sender is TextBox textBox)
        {
            viewModel.FinishRenamingCommand.Execute(textBox.DataContext);
        }
    }
}