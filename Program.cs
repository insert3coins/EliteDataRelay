using System;
using System.Windows.Forms;
using EliteCargoMonitor.Configuration;

namespace EliteCargoMonitor
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            AppConfiguration.Load();                        // Load settings from settings.json
            ApplicationConfiguration.Initialize();          // .NET 6+ helper
            Application.Run(new CargoForm());               // Show the form
        }
    }
}