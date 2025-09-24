namespace EliteDataRelay.Models
{
    /// <summary>
    /// Represents the base statistics for a Frame Shift Drive module.
    /// Using a record for immutable data transfer.
    /// </summary>
    public record FsdStats(double OptimalMass, double MaxFuelPerJump, double PowerConstant, double FuelMultiplier);

    /// <summary>
    /// Represents the calculated jump range values.
    /// </summary>
    public record JumpRangeResult(double Current, double Laden, double Max);
}