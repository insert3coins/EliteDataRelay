namespace EliteDataRelay.Models
{
    /// <summary>
    /// Represents a celestial body or station within a system.
    /// </summary>
    public class SystemEntity
    {
        /// <summary>
        /// The name of the body or station (e.g., "Procyon" or "Jameson Memorial").
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// The raw type from the journal used for icon lookups (e.g., "G-Type Star" or "Orbis").
        /// </summary>
        public string BodyType { get; set; } = string.Empty;
    }
}