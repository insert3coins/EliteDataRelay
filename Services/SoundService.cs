using System;
using System.Diagnostics;
using System.IO;
using System.Media;

namespace EliteCargoMonitor.Services
{
    public class SoundService : ISoundService, IDisposable
    {
        private readonly SoundPlayer _startSound;
        private readonly SoundPlayer _stopSound;

        public SoundService()
        {
            try
            {
                // Initialize sound players with embedded resources.
                // We copy the UnmanagedMemoryStream from resources into a managed MemoryStream.
                // This is a safer approach that avoids potential heap corruption issues (0xc0000374)
                // that can occur when SoundPlayer handles the lifetime of an unmanaged stream.
                _startSound = new SoundPlayer(CreateMemoryStreamFromResource(Properties.Resources.Start));
                _stopSound = new SoundPlayer(CreateMemoryStreamFromResource(Properties.Resources.Stop));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SoundService] Error initializing sound players: {ex}");
                // Create dummy sound players if resource loading fails
                _startSound = new SoundPlayer();
                _stopSound = new SoundPlayer();
            }
        }

        public void PlayStartSound()
        {
            try
            {
                _startSound.Play();
                Debug.WriteLine("[SoundService] Playing start sound");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SoundService] Error playing start sound: {ex}");
            }
        }

        public void PlayStopSound()
        {
            try
            {
                _stopSound.Play();
                Debug.WriteLine("[SoundService] Playing stop sound");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SoundService] Error playing stop sound: {ex}");
            }
        }

        private static MemoryStream CreateMemoryStreamFromResource(UnmanagedMemoryStream? sourceStream)
        {
            if (sourceStream is null) return new MemoryStream();

            // Do not dispose the sourceStream with 'using'. The ResourceManager, which
            // provides the stream, manages its lifetime. Disposing it here can lead
            // to memory corruption and crashes on application exit.
            var memoryStream = new MemoryStream();
            sourceStream.CopyTo(memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }

        public void Dispose()
        {
            _startSound?.Dispose();
            _stopSound?.Dispose();
        }
    }
}