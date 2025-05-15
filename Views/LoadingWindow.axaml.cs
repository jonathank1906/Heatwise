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
