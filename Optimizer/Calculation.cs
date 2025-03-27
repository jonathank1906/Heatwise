using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

class HeatProductionUnit
{
    public string Name { get; set; }
    public double MaxHeat { get; set; }
    public double ProductionCosts { get; set; }
    public double Emissions { get; set; }
    public double GasConsumption { get; set; }
    public double OilConsumption { get; set; }
    public double MaxElectricity { get; set; }
}

class HeatProductionData
{
    public List<HeatProductionUnit> Assets { get; set; }
}

class Optimizer
{
    private List<HeatProductionUnit> _units;

    public Optimizer(string jsonPath)
    {
        LoadData(jsonPath);
    }

    private void LoadData(string jsonPath)
    {
        string jsonData = File.ReadAllText(jsonPath);
        var data = JsonConvert.DeserializeObject<HeatProductionData>(jsonData);
        _units = data.Assets;
    }

    // Finds the cheapest combination of machines to meet the required heat
    public List<HeatProductionUnit> GetOptimalCombination(double requiredHeat)
    {
        var sortedUnits = _units.OrderBy(u => u.ProductionCosts / u.MaxHeat).ToList();
        return SelectMachines(sortedUnits, requiredHeat);
    }

    // Finds the lowest-emission combination of machines to meet the required heat
    public List<HeatProductionUnit> GetLowestEmissionsCombination(double requiredHeat)
    {
        var sortedUnits = _units.OrderBy(u => u.Emissions / u.MaxHeat).ToList();
        return SelectMachines(sortedUnits, requiredHeat);
    }

    // Helper function to select machines until required heat is met
    private List<HeatProductionUnit> SelectMachines(List<HeatProductionUnit> sortedUnits, double requiredHeat)
    {
        List<HeatProductionUnit> selectedUnits = new List<HeatProductionUnit>();
        double totalHeat = 0;

        foreach (var unit in sortedUnits)
        {
            if (totalHeat >= requiredHeat)
                break;

            selectedUnits.Add(unit);
            totalHeat += unit.MaxHeat;
        }

        return selectedUnits;
    }
}

class Program
{
    static void Main()
    {
        string jsonPath = "HeatProductionUnits.json";
        Optimizer optimizer = new Optimizer(jsonPath);

        double requiredHeat = 7.0; // Example heat demand

        // Get cheapest combination
        var bestCostCombination = optimizer.GetOptimalCombination(requiredHeat);
        Console.WriteLine("Optimal machine combination (lowest cost):");
        foreach (var unit in bestCostCombination)
        {
            Console.WriteLine($"- {unit.Name} (Cost: {unit.ProductionCosts}, Heat: {unit.MaxHeat})");
        }

        // Get lowest emissions combination
        var bestEmissionsCombination = optimizer.GetLowestEmissionsCombination(requiredHeat);
        Console.WriteLine("\nOptimal machine combination (lowest emissions):");
        foreach (var unit in bestEmissionsCombination)
        {
            Console.WriteLine($"- {unit.Name} (Emissions: {unit.Emissions}, Heat: {unit.MaxHeat})");
        }
    }
}
