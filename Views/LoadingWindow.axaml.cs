using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Heatwise.ViewModels;
using Heatwise.Models;
using Avalonia.Input;
using System.Runtime.InteropServices;

namespace Heatwise.Views;

public partial class LoadingWindow : Window
{
    public LoadingWindow()
    {
        InitializeComponent();
    }

    private void WindowDragMove(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}
