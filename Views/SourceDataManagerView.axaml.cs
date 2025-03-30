using Avalonia.Controls;
using ScottPlot.Avalonia;
using ScottPlot;

namespace Sem2Proj.Views;

public partial class SourceDataManagerView : UserControl
{
    public SourceDataManagerView()
    {
        InitializeComponent();

        //winter 1 Electricity price per day 00-01, winter demand 
        double[] dataA = { 1, 2, 3, 4, 5, 6, 7 }; // days
        //winter
        double[] dataB = { 1190.94, 615.31, 580.99, 557.82, 965.70, 773.11, 1010.17 }; //winter electricity price
        double[] dataY = { 6.62, 7.58, 6.8, 6.35, 6.05, 6.08, 6.15 }; //winter heat demand per day 
        //summer
        double[] dataX = { 752.03, 820.80, 843.68, 933.08, 738.53, 786.90, 757.50 };  // summer electricity price per day in Winter winter 
        double[] dataZ = { 1.79, 1.54, 1.4, 1.53, 1.7, 1.67, 1.71 }; //summer heat demand

        AvaPlot HeatDemand = this.Find<AvaPlot>("HeatDemand");
        
        // Apply dark mode styling
        HeatDemand.Plot.FigureBackground.Color = new Color("#1e1e1e");
        HeatDemand.Plot.Axes.Color(new Color("#888888"));
        
        // Grid styling
  
        
        HeatDemand.Plot.Grid.XAxisStyle.MajorLineStyle.Color = Colors.White.WithAlpha(15);
        HeatDemand.Plot.Grid.YAxisStyle.MajorLineStyle.Color = Colors.White.WithAlpha(15);
        HeatDemand.Plot.Grid.XAxisStyle.MinorLineStyle.Color = Colors.White.WithAlpha(5);
        HeatDemand.Plot.Grid.YAxisStyle.MinorLineStyle.Color = Colors.White.WithAlpha(5);
        
        HeatDemand.Plot.Grid.XAxisStyle.MinorLineStyle.Width = 1;
        HeatDemand.Plot.Grid.YAxisStyle.MinorLineStyle.Width = 1;
        
        // Add scatter plots with custom colors
        var scatter1 = HeatDemand.Plot.Add.Scatter(dataA, dataY);
        scatter1.LineWidth = 2;
        scatter1.Color = new Color("#2b9433"); // green for winter heat demand
        
        var scatter2 = HeatDemand.Plot.Add.Scatter(dataA, dataZ);
        scatter2.LineWidth = 2;
        scatter2.Color = new Color("#94332b"); // red for summer heat demand
        
        // Optionally add legend
        scatter1.Label = "Winter Heat Demand";
        scatter2.Label = "Summer Heat Demand";
        HeatDemand.Plot.ShowLegend();
        
        HeatDemand.Refresh();
    }
}