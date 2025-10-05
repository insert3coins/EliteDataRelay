using EliteDataRelay.Configuration;
using System;
using System.Diagnostics;
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

            // Load user settings from the configuration file.
            AppConfiguration.Load();

            // To customize application configuration such as high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            // Configure a trace listener to write all Debug.WriteLine output to a file.
            // This is essential for capturing logs when not running with a debugger attached.
            var logFilePath = Path.Combine(AppConfiguration.AppDataPath, "debug_log.txt");

            // Overwrite/trim the log file on each application start to keep it clean.
            try { File.Delete(logFilePath); }
            catch (IOException) { /* Ignore if file is locked, logging will append */ }
            catch (UnauthorizedAccessException) { /* Ignore if no permissions */ }

            var listener = new TextWriterTraceListener(logFilePath);
            // Add the listener to both Trace and Debug to capture all diagnostic output.
            // Both Debug and Trace write to the same shared Listeners collection.
            Trace.Listeners.Add(listener);

            // Ensure messages are written to the file immediately.
            Trace.AutoFlush = true;

            // Create and run the main form. The form itself is now responsible
            // for creating and managing its own services.
            Application.Run(new CargoForm());
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