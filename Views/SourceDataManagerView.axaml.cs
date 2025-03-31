using Avalonia.Controls;
using ScottPlot.Avalonia;
using ScottPlot;
using System.Linq;
using Avalonia.Interactivity;
using Avalonia.Controls;
using ScottPlot.Avalonia;
using ScottPlot;
using System.Linq;
using Avalonia.Interactivity;
using System;
using Avalonia.Controls.Primitives;
using System.Collections.Generic;
using System.Collections.ObjectModel;


namespace Sem2Proj.Views;

public partial class SourceDataManagerView : UserControl
{
     private readonly SourceDataManager _sourceDataManager = new SourceDataManager();
    public SourceDataManagerView()
    {
        InitializeComponent();
        PlotHeatDemandData();
        LoadBlackoutDates();
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
private void LoadBlackoutDates()
{
    var calendar = this.Find<Calendar>("MyCalendar");
    if (calendar == null) return;

    var dataPoints = _sourceDataManager.GetWinterHeatDemandData();
    var datesWithData = dataPoints.Select(x => x.timestamp.Date).Distinct().ToList();

    if (!datesWithData.Any()) return;

    var minDate = datesWithData.Min();
    var maxDate = datesWithData.Max();

    // Set calendar display range
    calendar.DisplayDateStart = minDate;
    calendar.DisplayDateEnd = maxDate;

    // Clear existing blackout dates
    calendar.BlackoutDates.Clear();

    // Generate all dates in the range
    var allDates = new List<DateTime>();
    for (var date = minDate; date <= maxDate; date = date.AddDays(1))
    {
        allDates.Add(date);
    }

    // Find dates WITHOUT data
    var datesWithoutData = allDates.Except(datesWithData).ToList();

    if (datesWithoutData.Count == 0) return;

    // Group consecutive missing dates into ranges
    DateTime? rangeStart = null;
    DateTime? rangeEnd = null;

    foreach (var date in datesWithoutData.OrderBy(d => d))
    {
        if (!rangeStart.HasValue)
        {
            rangeStart = date;
            rangeEnd = date;
        }
        else if (date == rangeEnd.Value.AddDays(1))
        {
            rangeEnd = date; // Extend the current range
        }
        else
        {
            // Add the current range to blackout dates
            calendar.BlackoutDates.Add(new CalendarDateRange(rangeStart.Value, rangeEnd.Value));
            rangeStart = date;
            rangeEnd = date;
        }
    }

    // Add the final range if it exists
    if (rangeStart.HasValue)
    {
        calendar.BlackoutDates.Add(new CalendarDateRange(rangeStart.Value, rangeEnd.Value));
    }
}
}