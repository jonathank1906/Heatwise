using FluentAvalonia.UI.Windowing;
using Avalonia.Input;
namespace Sem2Proj.Views;

public partial class MainWindow : AppWindow
{
    public MainWindow()
    {
        InitializeComponent();
        PointerPressed += OnPointerPressed;
        TitleBar.ExtendsContentIntoTitleBar = true;
        TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && e.GetPosition(this).Y <= TopHeader.Height)
        {
            BeginMoveDrag(e);
        }
    }
}