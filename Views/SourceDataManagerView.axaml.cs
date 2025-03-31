using Avalonia.Controls;
using ScottPlot.Avalonia;
using ScottPlot;
using System.Linq;
using Avalonia.Interactivity;

namespace Sem2Proj.Views;

public partial class SourceDataManagerView : UserControl
{
    public SourceDataManagerView()
    {
        InitializeComponent();
        PlotHeatDemandData();
    }

    private void PlotHeatDemandData()
    {
        var dataManager = new SourceDataManager();
        var heatDemandData = dataManager.GetWinterHeatDemandData();

        double[] timestamps = heatDemandData.Select(x => x.timestamp.ToOADate()).ToArray();
        double[] values = heatDemandData.Select(x => x.value).ToArray();

        AvaPlot HeatDemand = this.Find<AvaPlot>("HeatDemand");
        HeatDemand.UserInputProcessor.DoubleLeftClickBenchmark(false);  
        HeatDemand.Plot.Clear();

        var bgColor = new Color("#1e1e1e");
        HeatDemand.Plot.FigureBackground.Color = bgColor;
        HeatDemand.Plot.DataBackground.Color = bgColor;
        HeatDemand.Plot.Axes.DateTimeTicksBottom();
        HeatDemand.Plot.Axes.Color(new Color("#FFFFFF"));

        HeatDemand.Plot.Grid.XAxisStyle.MajorLineStyle.Color = Colors.White.WithAlpha(25);
        HeatDemand.Plot.Grid.YAxisStyle.MajorLineStyle.Color = Colors.White.WithAlpha(25);
        HeatDemand.Plot.Grid.XAxisStyle.MajorLineStyle.Width = 1.5f;
        HeatDemand.Plot.Grid.YAxisStyle.MajorLineStyle.Width = 1.5f;

        if (timestamps.Length > 0 && values.Length > 0)
        {
            var scatter = HeatDemand.Plot.Add.Scatter(timestamps, values);
            scatter.Color = Colors.LightSkyBlue; 
            scatter.MarkerSize = 5;             
            scatter.LineWidth = 2;              
            HeatDemand.Refresh();
        }
    }
    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        ResetPlotView();  
    }
    private void ResetPlotView()
    {
        AvaPlot HeatDemand = this.Find<AvaPlot>("HeatDemand");
        HeatDemand.Plot.Axes.AutoScale(); 
        HeatDemand.Refresh();
    }
}