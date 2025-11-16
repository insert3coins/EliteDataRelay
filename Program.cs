using EliteDataRelay.Configuration;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;

namespace EliteDataRelay
{
    static class Program
    {
        private static readonly string[] ActivationEventNames = new[]
        {
            "Global/EliteDataRelay_Activate",
            "EliteDataRelay_Activate_Local"
        };

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Single-instance guard: prevent multiple instances from running.
            bool createdNew;
            using var singleInstanceMutex = new Mutex(true, "Global/EliteDataRelay_SingleInstance", out createdNew);
            if (!createdNew)
            {
                // Signal the existing instance to bring its window to the foreground, then exit.
                if (!TrySignalExistingInstance())
                {
                    // If signaling fails (older instance without listener), fall back to a quick notice and exit
                    try
                    {
                        MessageBox.Show(
                            "Elite Data Relay is already running.",
                            "Elite Data Relay",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    catch { /* ignore if UI not available */ }
                }
                return;
            }

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

            // Create the activation event and listener so subsequent launches can bring this window to front.
            EventWaitHandle? activationEvent = null;
            try
            {
                activationEvent = CreateActivationEvent();
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[Program] Failed to create activation event: {ex}");
            }

            // Create the main form instance so we can activate it on signal.
            var mainForm = new CargoForm();
            if (activationEvent != null)
            {
                StartActivationListener(mainForm, activationEvent);
            }
            else
            {
                Trace.WriteLine("[Program] Activation event unavailable. Global activation requests will be ignored.");
            }

            try
            {
                // Run the application.
                Application.Run(mainForm);
            }
            finally
            {
                activationEvent?.Dispose();
            }
        }

        private static void StartActivationListener(Form mainForm, EventWaitHandle activationEvent)
        {
            var thread = new Thread(() =>
            {
                try
                {
                    while (!mainForm.IsDisposed)
                    {
                        if (!activationEvent.WaitOne(Timeout.Infinite))
                            continue;

                        if (mainForm.IsDisposed)
                            break;

                        try
                        {
                            mainForm.BeginInvoke(new MethodInvoker(() =>
                            {
                                try
                                {
                                    if (mainForm.WindowState == FormWindowState.Minimized)
                                    {
                                        mainForm.WindowState = FormWindowState.Normal;
                                    }
                                    // Ensure visible, bring to front, and focus
                                    mainForm.Show();
                                    mainForm.Activate();
                                    // Toggle TopMost to force Z-order raise without stealing focus aggressively
                                    bool prevTopMost = mainForm.TopMost;
                                    mainForm.TopMost = true;
                                    mainForm.TopMost = prevTopMost;
                                    // Try native focus/restore
                                    ShowWindow(mainForm.Handle, SW_RESTORE);
                                    SetForegroundWindow(mainForm.Handle);
                                }
                                catch { }
                            }));
                        }
                        catch { }
                    }
                }
                catch { }
            })
            { IsBackground = true, Name = "EDR-ActivationListener" };
            thread.Start();
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;

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

        private static bool TrySignalExistingInstance()
        {
            foreach (var name in ActivationEventNames)
            {
                try
                {
                    using var evt = EventWaitHandle.OpenExisting(name);
                    evt.Set();
                    return true;
                }
                catch (WaitHandleCannotBeOpenedException)
                {
                    continue;
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }
                catch
                {
                    continue;
                }
            }
            return false;
        }

        private static EventWaitHandle? CreateActivationEvent()
        {
            foreach (var name in ActivationEventNames)
            {
                try
                {
                    return new EventWaitHandle(false, EventResetMode.AutoReset, name);
                }
                catch (UnauthorizedAccessException ex)
                {
                    Trace.WriteLine($"[Program] Access denied creating activation event '{name}': {ex.Message}");
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"[Program] Failed to create activation event '{name}': {ex.Message}");
                }
            }
            return null;
        }
    }
}
