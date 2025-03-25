using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using LiveChartsCore.Defaults;

namespace Sem2Proj.ViewModels;

public partial class OptimizerViewModel : ViewModelBase
{
    private const int OpenWidth = 275;
    private const int ClosedWidth = 0;
    
    [ObservableProperty]
    private double _paneWidth = OpenWidth;
    
    [ObservableProperty]
    private string _toggleSymbol = "←";
    
    [ObservableProperty]
    private bool _isPaneOpen = true;

    [RelayCommand]
    private async Task TriggerPane()
    {
        IsPaneOpen = !IsPaneOpen;
        ToggleSymbol = IsPaneOpen ? "←" : "≡";
        
        // Animate the width change
        var targetWidth = IsPaneOpen ? OpenWidth : ClosedWidth;
        var step = (targetWidth - PaneWidth) / 10;
        
        for (int i = 0; i < 10; i++)
        {
            PaneWidth += step;
            await Task.Delay(10);
        }
        PaneWidth = targetWidth; // Ensure exact final value
    }


    private readonly DatabaseHandler _dbHandler;

    [ObservableProperty]
    private string title = "Heat Demand Over Time";

    [ObservableProperty]
    private IList<ISeries> series = new List<ISeries>();

    [ObservableProperty]
    private IList<Axis> xAxes = new List<Axis>();

    [ObservableProperty]
    private IList<Axis> yAxes = new List<Axis>();

    public OptimizerViewModel()
    {
        // For LiveCharts version 2.0.0-rc5.4, we don't need to configure the mapper
        // We'll use DateTimeAxis directly

        _dbHandler = new DatabaseHandler();
        LoadData();
        InitializeAxes();
    }

    private void LoadData()
    {
        try
        {
            var data = _dbHandler.GetData();
            if (data != null && data.Count > 0)
            {
                Console.WriteLine($"GraphViewModel received {data.Count} data points");
                Console.WriteLine($"First point: {data[0].timestamp}, {data[0].value}");
                Console.WriteLine($"Last point: {data[data.Count-1].timestamp}, {data[data.Count-1].value}");

                // Create a LineSeries with the data points
                var lineSeries = new LineSeries<ObservablePoint>
                {
                    Values = data.Select(point => new ObservablePoint(point.timestamp.ToOADate(), point.value)).ToList(),
                    Fill = null,
                    GeometrySize = 0,
                    LineSmoothness = 0,
                    Name = "Heat Demand"
                };

                Series = new List<ISeries> { lineSeries };
            }
            else
            {
                Console.WriteLine("No data to display");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in LoadData: {ex.Message}");
        }
    }

    private void InitializeAxes()
    {
        XAxes = new List<Axis>
        {
            new Axis
            {
                Name = "Time",
                NamePaint = new SolidColorPaint(SKColors.Black),
                LabelsPaint = new SolidColorPaint(SKColors.Black),
                Labeler = value => 
                {
                    try
                    {
                        // Convert OADate back to DateTime for display
                        return DateTime.FromOADate(value).ToString("HH:mm");
                    }
                    catch
                    {
                        return value.ToString("0.0"); // Fallback for invalid values
                    }
                },
                MinStep = 1.0 / 24.0  // Step size of 1 hour (1/24 of a day)
            }
        };

        YAxes = new List<Axis>
        {
            new Axis
            {
                Name = "Heat Demand (MWh)",
                NamePaint = new SolidColorPaint(SKColors.Black),
                LabelsPaint = new SolidColorPaint(SKColors.Black)
            }
        };
    }
    
}