using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Sem2Proj.ViewModels;
using Sem2Proj.Models;
using Avalonia.Input;
using System.Runtime.InteropServices;

namespace Sem2Proj.Views;

public partial class LoadingWindow : Window
{
    public LoadingWindow()
    {
        // Not to be used.
        // Avalonia won't shut up without this argumentless constructor.
        InitializeComponent();
    }
    public LoadingWindow(LoadingWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        StartLoading();
    }

    private async void StartLoading()
    {
        var viewModel = DataContext as LoadingWindowViewModel;
        if (viewModel != null)
        {
            await viewModel.LoadApplicationAsync();
            this.Close();
        }

    }

    private void WindowDragMove(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}
