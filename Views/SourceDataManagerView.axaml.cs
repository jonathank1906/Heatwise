using Avalonia.Controls;
using ScottPlot.Avalonia;
using ScottPlot;
using System.Linq;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;

namespace Sem2Proj.Views;

public partial class SourceDataManagerView : UserControl
{
    private readonly SourceDataManager _sourceDataManager = new SourceDataManager();
    
    public SourceDataManagerView()
    {
        InitializeComponent();
        InitializePlots();
        LoadBlackoutDates();
    }

    private void InitializePlots()
    {
        // Winter data
        PlotData(this.Find<AvaPlot>("WinterHeatPlot"), 
                _sourceDataManager.GetWinterHeatDemandData(), 
                Colors.LightSkyBlue, 
                "Heat Demand",
                "Heat Demand (MWh)");
                
        PlotData(this.Find<AvaPlot>("WinterElectricityPlot"), 
                _sourceDataManager.GetWinterElectricityPriceData(), 
                Colors.LightGreen, 
                "Electricity Price",
                "Electricity Price (DKK/MWh)");
        
        // Summer data
        PlotData(this.Find<AvaPlot>("SummerHeatPlot"), 
                _sourceDataManager.GetSummerHeatDemandData(), 
                Colors.LightSkyBlue, 
                "Heat Demand",
                "Heat Demand (MWh)");
                
        PlotData(this.Find<AvaPlot>("SummerElectricityPlot"), 
                _sourceDataManager.GetSummerElectricityPriceData(), 
                Colors.LightGreen, 
                "Electricity Price",
                "Electricity Price (DKK/MWh)");
    }

    private void PlotData(AvaPlot plot, IEnumerable<(DateTime timestamp, double value)> data, 
                        Color color, string label, string yAxisLabel)
    {
        plot.Plot.Clear();
        
        double[] timestamps = data.Select(x => x.timestamp.ToOADate()).ToArray();
        double[] values = data.Select(x => x.value).ToArray();

        var scatter = plot.Plot.Add.Scatter(timestamps, values);
        scatter.Color = color;
        scatter.Label = label;
        scatter.MarkerSize = 5;
        scatter.LineWidth = 2;

        // Apply consistent styling
        var bgColor = new Color("#1e1e1e");
        plot.Plot.FigureBackground.Color = bgColor;
        plot.Plot.DataBackground.Color = bgColor;
        
        // Axis labels and styling
        plot.Plot.XLabel("Time & Date");
        plot.Plot.YLabel(yAxisLabel);
        plot.Plot.Title(label);
  
        
        plot.Plot.Axes.DateTimeTicksBottom();
        plot.Plot.Axes.Color(new Color("#FFFFFF"));
        plot.Plot.Grid.XAxisStyle.MajorLineStyle.Color = Colors.White.WithAlpha(25);
        plot.Plot.Grid.YAxisStyle.MajorLineStyle.Color = Colors.White.WithAlpha(25);
        plot.Plot.Grid.XAxisStyle.MajorLineStyle.Width = 1.5f;
        plot.Plot.Grid.YAxisStyle.MajorLineStyle.Width = 1.5f;
        
        plot.Plot.Axes.AutoScale();
        plot.Refresh();
    }

    private void SetWinterRange_Click(object sender, RoutedEventArgs e)
    {
        FilterData(
            this.Find<Calendar>("WinterCalendar"),
            this.Find<AvaPlot>("WinterHeatPlot"),
            _sourceDataManager.GetWinterHeatDemandData(),
            Colors.LightSkyBlue,
            "Heat Demand",
            "Heat Demand (MWh)");
            
        FilterData(
            this.Find<Calendar>("WinterCalendar"),
            this.Find<AvaPlot>("WinterElectricityPlot"),
            _sourceDataManager.GetWinterElectricityPriceData(),
            Colors.LightGreen,
            "Electricity Price",
            "Electricity Price (DKK/MWh)");
    }

    private void SetSummerRange_Click(object sender, RoutedEventArgs e)
    {
        FilterData(
            this.Find<Calendar>("SummerCalendar"),
            this.Find<AvaPlot>("SummerHeatPlot"),
            _sourceDataManager.GetSummerHeatDemandData(),
            Colors.LightSkyBlue,
            "Heat Demand",
            "Heat Demand (MWh)");
            
        FilterData(
            this.Find<Calendar>("SummerCalendar"),
            this.Find<AvaPlot>("SummerElectricityPlot"),
            _sourceDataManager.GetSummerElectricityPriceData(),
            Colors.LightGreen,
            "Electricity Price",
            "Electricity Price (DKK/MWh)");
    }

    private void ResetWinterView_Click(object sender, RoutedEventArgs e)
    {
        PlotData(this.Find<AvaPlot>("WinterHeatPlot"), 
                _sourceDataManager.GetWinterHeatDemandData(), 
                Colors.LightSkyBlue, 
                "Heat Demand",
                "Heat Demand (MWh)");
                
        PlotData(this.Find<AvaPlot>("WinterElectricityPlot"), 
                _sourceDataManager.GetWinterElectricityPriceData(), 
                Colors.LightGreen, 
                "Electricity Price",
                "Electricity Price (DKK/MWh)");
    }

    private void ResetSummerView_Click(object sender, RoutedEventArgs e)
    {
        PlotData(this.Find<AvaPlot>("SummerHeatPlot"), 
                _sourceDataManager.GetSummerHeatDemandData(), 
                Colors.LightSkyBlue, 
                "Heat Demand",
                "Heat Demand (MWh)");
                
        PlotData(this.Find<AvaPlot>("SummerElectricityPlot"), 
                _sourceDataManager.GetSummerElectricityPriceData(), 
                Colors.LightGreen, 
                "Electricity Price",
                "Electricity Price (DKK/MWh)");
    }

    private void FilterData(Calendar calendar, AvaPlot plot, 
                          IEnumerable<(DateTime timestamp, double value)> fullData,
                          Color color, string label, string yAxisLabel)
    {
        if (calendar.SelectedDates.Count == 0) return;
        
        var selectedDates = calendar.SelectedDates.OrderBy(d => d).ToList();
        DateTime startDate = selectedDates.First();
        DateTime endDate = selectedDates.Last().AddDays(1);

        var filteredData = fullData
            .Where(x => x.timestamp >= startDate && x.timestamp <= endDate)
            .ToList();

        if (filteredData.Count == 0) return;

        PlotData(plot, filteredData, color, label, yAxisLabel);
        plot.Plot.Axes.SetLimitsX(startDate.ToOADate(), endDate.ToOADate());
        plot.Refresh();
    }

    private void LoadBlackoutDates()
    {
        LoadCalendarBlackoutDates(this.Find<Calendar>("WinterCalendar"));
        LoadCalendarBlackoutDates(this.Find<Calendar>("SummerCalendar"));
    }

    private void LoadCalendarBlackoutDates(Calendar calendar)
    {
        if (calendar == null) return;

        var winterDataPoints = _sourceDataManager.GetWinterHeatDemandData();
        var summerDataPoints = _sourceDataManager.GetSummerHeatDemandData();

        var datesWithData = winterDataPoints.Select(x => x.timestamp.Date)
                         .Concat(summerDataPoints.Select(x => x.timestamp.Date))
                         .Distinct()
                         .ToList();

        if (!datesWithData.Any()) return;

        var minDate = datesWithData.Min();
        var maxDate = datesWithData.Max();

        calendar.DisplayDateStart = minDate;
        calendar.DisplayDateEnd = maxDate;
        calendar.BlackoutDates.Clear();

        var allDates = new List<DateTime>();
        for (var date = minDate; date <= maxDate; date = date.AddDays(1))
        {
            allDates.Add(date);
        }

        var datesWithoutData = allDates.Except(datesWithData).ToList();
        if (datesWithoutData.Count == 0) return;

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
                rangeEnd = date;
            }
            else
            {
                calendar.BlackoutDates.Add(new CalendarDateRange(rangeStart.Value, rangeEnd.Value));
                rangeStart = date;
                rangeEnd = date;
            }
        }

        if (rangeStart.HasValue)
        {
            calendar.BlackoutDates.Add(new CalendarDateRange(rangeStart.Value, rangeEnd.Value));
        }
    }
}