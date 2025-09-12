using System;
using System.Windows.Forms;

namespace EliteCargoMonitor
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();          // .NET 6+ helper
            Application.Run(new CargoForm());               // Show the form
        }
    }
}