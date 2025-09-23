namespace EliteDataRelay.Models
{
    public class FsdStats
    {
        public double OptimalMass { get; }
        public double MaxFuelPerJump { get; }
        public double PowerConstant { get; }
        public double FuelMultiplier { get; }

        public FsdStats(double optimalMass, double maxFuelPerJump, double powerConstant, double fuelMultiplier)
        {
            OptimalMass = optimalMass;
            MaxFuelPerJump = maxFuelPerJump;
            PowerConstant = powerConstant;
            FuelMultiplier = fuelMultiplier;
        }
    }

    public class JumpRangeResult
    {
        public double Current { get; set; }
        public double Laden { get; set; }
        public double Max { get; set; }
    }
}