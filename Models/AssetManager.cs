namespace Sem2Proj;

public class HeatAsset
{
    public string Name { get; set; } = ""; // Name of the asset
    public double MaxHeat { get; set; } //MWh
    public double ProductionCosts { get; set; } //DKK/MWh(th)
    public double CO2Emissions { get; set; } //kg/MWh(th)

    // Gas boiler
    public class GasBoiler : HeatAsset
    {
        public double GasConsumption { get; set; } // MWh(gas)/MWh(th)
    }

    // Oil boiler
    public class OilBoiler : HeatAsset
    {
        public double OilConsumption { get; set; } // MWh(oil)/MWh(th)
    }

    // Gas motor (produces heat & electricity)
    public class GasMotor : HeatAsset
    {
        public double MaxElectricity { get; set; } // in MW
        public double GasConsumption { get; set; } // MWh(gas)/MWh(th)
    }

    // Heat pump (uses electricity to generate heat)
    public class HeatPump : HeatAsset
    {
        public double MaxElectricity { get; set; } // in MW
    }
}