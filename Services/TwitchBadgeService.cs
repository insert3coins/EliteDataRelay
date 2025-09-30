using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace EliteDataRelay.Services
{
    public class TwitchBadgeService : IDisposable
    {
        private readonly HttpClient _httpClient = new HttpClient();

        // Cache for the actual badge images
        private readonly Dictionary<string, Image> _badgeImageCache = new Dictionary<string, Image>();

        public TwitchBadgeService(ITwitchService twitchService)
        {
            // Constructor is kept for dependency injection consistency, but the service is now self-contained.
        }

        public async Task<List<Image>> GetBadgesForUser(List<KeyValuePair<string, string>>? badgeInfo)
        {
            var badgeImages = new List<Image>();
            if (badgeInfo == null || !badgeInfo.Any())
            {
                return badgeImages;
            }

            foreach (var userBadge in badgeInfo)
            {
                // The 'badge-info' tag from Twitch provides the URL directly in the Value part of the KeyValuePair.
                // The Key is the badge name/version (e.g., "subscriber/6"), and the Value is the image URL.
                var badgeUrl = userBadge.Value;

                if (!string.IsNullOrEmpty(badgeUrl))
                {
                    var badgeImage = await GetBadgeImage(badgeUrl);
                    if (badgeImage != null)
                    {
                        badgeImages.Add(badgeImage);
                    }
                }
            }

            return badgeImages;
        }

        private async Task<Image?> GetBadgeImage(string url)
        {
            if (_badgeImageCache.TryGetValue(url, out var cachedImage))
            {
                return cachedImage;
            }

            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        var image = Image.FromStream(stream);
                        _badgeImageCache[url] = image;
                        return image;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TwitchBadgeService] Failed to download badge image from {url}: {ex.Message}");
            }

            return null;
        }

        public void Dispose()
        {
            _httpClient.Dispose();
            foreach (var image in _badgeImageCache.Values)
            {
                image.Dispose();
            }
            _badgeImageCache.Clear();
            GC.SuppressFinalize(this);
        }
    }
}