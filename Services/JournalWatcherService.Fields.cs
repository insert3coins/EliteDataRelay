using System.Text.Json;

namespace EliteDataRelay.Services
{
    public partial class JournalWatcherService
    {
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
    }
}