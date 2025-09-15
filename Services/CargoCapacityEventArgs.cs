using System;

namespace EliteCargoMonitor.Services
{
    public class CargoCapacityEventArgs : EventArgs
    {
        public int CargoCapacity { get; }

        public CargoCapacityEventArgs(int cargoCapacity)
        {
            CargoCapacity = cargoCapacity;
        }
    }
}