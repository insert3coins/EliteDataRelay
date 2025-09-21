using EliteDataRelay.Configuration;
using System;
using System.IO;
using System.Windows.Forms;

namespace EliteDataRelay
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Add global exception handlers to catch any unhandled errors.
            // This is crucial for preventing the application from silently crashing on startup.
            Application.ThreadException += (sender, e) => HandleUnhandledException(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => HandleUnhandledException(e.ExceptionObject as Exception);

            // To customize application configuration such as high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            // Configure and run the main form using a dependency container
            // to ensure all services are correctly instantiated and passed to the form.
            var container = new DependencyContainer();
            var mainForm = container.CreateMainForm();

            Application.Run(mainForm);
        }

        private static void HandleUnhandledException(Exception? ex)
        {
            if (ex == null) return;

            string errorMessage = $"A critical error occurred and the application must close.\n\n" +
                                  $"Error: {ex.Message}\n\n" +
                                  $"A detailed crash log has been saved to:\n" +
                                  $"{Path.Combine(AppConfiguration.AppDataPath, "crash_log.txt")}";

            try
            {
                // Attempt to log the full exception details to a file.
                string logMessage = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC] - Unhandled Exception\n" +
                                    $"============================================================\n" +
                                    $"Message: {ex.Message}\n" +
                                    $"------------------------------------------------------------\n" +
                                    $"Stack Trace:\n{ex.StackTrace}\n" +
                                    $"============================================================\n\n";

                File.AppendAllText(Path.Combine(AppConfiguration.AppDataPath, "crash_log.txt"), logMessage);
            }
            catch (Exception logEx)
            {
                // If logging fails, add a note to the error message.
                errorMessage += $"\n\n(Note: Failed to write to crash log file: {logEx.Message})";
            }

            MessageBox.Show(errorMessage, "Elite Data Relay - Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            Environment.Exit(1); // Ensure the application terminates.
        }
    }
}