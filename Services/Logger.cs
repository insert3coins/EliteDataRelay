using System.Diagnostics;
using EliteDataRelay.Configuration;

namespace EliteDataRelay.Services
{
    public static class Logger
    {
        public static void Info(string message)
        {
            Trace.WriteLine(message);
        }

        public static void Verbose(string message)
        {
            if (AppConfiguration.VerboseLogging)
            {
                Trace.WriteLine(message);
            }
        }
    }
}


