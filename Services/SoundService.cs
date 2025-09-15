using System;
using System.Diagnostics;
using System.IO;
using System.Media;

namespace EliteCargoMonitor.Services
{
    /// <summary>
    /// Service for managing application sound effects
    /// </summary>
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

        /// <summary>
        /// Play the start monitoring sound
        /// </summary>
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

        /// <summary>
        /// Play the stop monitoring sound
        /// </summary>
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

        /// <summary>
        /// Creates a managed <see cref="MemoryStream"/> from an unmanaged resource stream.
        /// </summary>
        /// <param name="sourceStream">The source <see cref="UnmanagedMemoryStream"/> from application resources.</param>
        /// <returns>A new <see cref="MemoryStream"/> containing the sound data.</returns>
        /// <remarks>
        /// This method prevents potential heap corruption by copying the data from the
        /// unmanaged stream into a managed one. The <see cref="SoundPlayer"/> can then safely
        /// manage the lifetime of the <see cref="MemoryStream"/>. The source stream is disposed of.
        /// </remarks>
        private static MemoryStream CreateMemoryStreamFromResource(UnmanagedMemoryStream? sourceStream)
        {
            if (sourceStream is null) return new MemoryStream();

            using (sourceStream)
            {
                var memoryStream = new MemoryStream();
                sourceStream.CopyTo(memoryStream);
                memoryStream.Position = 0;
                return memoryStream;
            }
        }

        public void Dispose()
        {
            _startSound?.Dispose();
            _stopSound?.Dispose();
        }
    }
}