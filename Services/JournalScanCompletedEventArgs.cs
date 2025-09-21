using System;

namespace EliteDataRelay.Services
{
    public class JournalScanCompletedEventArgs : EventArgs
    {
        public bool Success { get; }
        public int FilesScanned { get; }
        public int NewSystemsFound { get; }
        public string ErrorMessage { get; }

        public JournalScanCompletedEventArgs(int filesScanned, int newSystemsFound)
        {
            Success = true;
            FilesScanned = filesScanned;
            NewSystemsFound = newSystemsFound;
            ErrorMessage = string.Empty;
        }

        public JournalScanCompletedEventArgs(string errorMessage)
        {
            Success = false;
            ErrorMessage = errorMessage;
        }
    }
}