namespace EliteDataRelay.Services
{
    /// <summary>
    /// Service interface for managing application sound effects
    /// </summary>
    public interface ISoundService
    {
        /// <summary>
        /// Play the start monitoring sound
        /// </summary>
        void PlayStartSound();

        /// <summary>
        /// Play the stop monitoring sound
        /// </summary>
        void PlayStopSound();

    }
}
