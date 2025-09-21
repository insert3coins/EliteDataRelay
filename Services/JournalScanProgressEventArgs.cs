using System;

namespace EliteDataRelay.Services
{
    public class JournalScanProgressEventArgs : EventArgs
    {
        public int FilesProcessed { get; }
        public int TotalFiles { get; }
        public JournalScanProgressEventArgs(int filesProcessed, int totalFiles)
        {
            FilesProcessed = filesProcessed;
            TotalFiles = totalFiles;
        }
    }
}