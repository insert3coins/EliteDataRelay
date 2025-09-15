using System;
using System.Diagnostics;
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
                // Initialize sound players with embedded resources
                _startSound = new SoundPlayer(Properties.Resources.Start);
                _stopSound = new SoundPlayer(Properties.Resources.Stop);
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

        public void Dispose()
        {
            _startSound?.Dispose();
            _stopSound?.Dispose();
        }
    }
}