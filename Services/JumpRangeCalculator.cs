using EliteDataRelay.Models;
using System;
using System.Linq;

namespace EliteDataRelay.Services

{
    public static class JumpRangeCalculator
    {
        public static JumpRangeResult? Calculate(ShipLoadout? loadout, Status? status)
        {
            if (loadout == null) return null;

            var fsdModule = loadout.Modules.FirstOrDefault(m => m.Slot == "FrameShiftDrive");
            if (fsdModule == null) return null;

            var baseFsdStats = FsdDataProvider.GetFsdStats(fsdModule.Item);
            if (baseFsdStats == null) return null; // FSD not in our database

            // Get FSD optimal mass, applying engineering modifiers
            double optimalMass = GetModifiedValue(fsdModule, "FSDOptimalMass", baseFsdStats.OptimalMass);

            // Max fuel per jump is a base stat of the FSD and not typically modified by engineering in a way that affects this formula directly.
            double maxFuelPerJump = baseFsdStats.MaxFuelPerJump;

            // The journal's 'UnladenMass' is the total mass of the ship including hull and all modules (with empty tanks).
            // We add 1T for the commander, which is part of the ship's total mass for jump calculations.
            double shipBaseMass = loadout.UnladenMass + 1.0;

            // Calculate current mass
            double currentMass = shipBaseMass;
            if (status != null)
            {
                // The reserve fuel tank does not contribute to mass for jump calculations.
                currentMass += (status.Fuel?.FuelMain ?? 0) + status.Cargo;
            }

            // Calculate laden mass (full fuel, full cargo)
            double maxFuelInMainTank = loadout.FuelCapacity?.Main ?? 0;
            double maxCargo = loadout.CargoCapacity;
            double ladenMass = shipBaseMass + maxFuelInMainTank + maxCargo;

            // Calculate jump ranges
            double currentRange = CalculateSingleRange(currentMass, optimalMass, maxFuelPerJump, baseFsdStats);
            double ladenRange = CalculateSingleRange(ladenMass, optimalMass, maxFuelPerJump, baseFsdStats);

            return new JumpRangeResult(currentRange, ladenRange, loadout.MaxJumpRange);
        }

        private static double CalculateSingleRange(double totalMass, double optimalMass, double maxFuelPerJump, FsdStats baseFsdStats)
        {
            if (totalMass <= 0) return 0;

            // Formula based on https://forums.frontier.co.uk/threads/the-great-jump-range-formula-thread.84579/
            // Range = (MaxFuelPerJump / FuelMultiplier) * (OptimalMass / TotalMass) ^ PowerConstant
            double range = (maxFuelPerJump / baseFsdStats.FuelMultiplier) * Math.Pow(optimalMass / totalMass, baseFsdStats.PowerConstant);
            return Math.Floor(range * 100) / 100;
        }

        private static double GetModifiedValue(ShipModule module, string label, double baseValue)
        {
            if (module.Engineering?.Modifiers != null)
            {
                var modifier = module.Engineering.Modifiers.FirstOrDefault(m => m.Label.Equals(label, StringComparison.OrdinalIgnoreCase));
                if (modifier != null)
                {
                    return modifier.Value;
                }
            }
            return baseValue;
        }
    }
}
