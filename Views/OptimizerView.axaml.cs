using FluentAvalonia.UI.Windowing;
using Avalonia.Controls;

using Sem2Proj.ViewModels;
namespace Sem2Proj.Views;

public partial class OptimizerView : UserControl
{
    public OptimizerView()
    {
        InitializeComponent();
        DataContext = new OptimizerViewModel(); 
    }
}