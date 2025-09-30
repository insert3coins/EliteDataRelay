using EliteDataRelay.Configuration;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EliteDataRelay.Services
{
    /// <summary>
    /// Handles the Twitch OAuth 2.0 Authorization Code Flow to securely obtain user tokens.
    /// </summary>
    public static class TwitchAuthService
    {
        // IMPORTANT: For a real application, you should register your own client ID with Twitch.
        // This is a public client ID intended for use with local development and the Authorization Code Flow.
        // It is configured to allow http://localhost redirects.
        // This one is from the official Twitch "Sample Go" application and is known to work.
        private const string ClientId = "8uu6pvwkbmw520q4t86e3t1dsnt7rp";
        private const string RedirectUri = "http://localhost:8899/redirect/";

        public static async Task<bool> LoginToTwitch()
        {
            try
            {
                // 1. Get the authorization code from the user.
                string? authCode = await GetAuthorizationCodeAsync();
                if (string.IsNullOrEmpty(authCode))
                {
                    return false; // User cancelled or an error occurred.
                }

                // 2. Exchange the authorization code for an access token.
                return await ExchangeCodeForTokenAsync(authCode);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TwitchAuthService] Login failed: {ex.Message}");
                MessageBox.Show($"Twitch login failed: {ex.Message}", "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private static async Task<string?> GetAuthorizationCodeAsync()
        {
            using var listener = new HttpListener();
            listener.Prefixes.Add(RedirectUri);
            listener.Start();

            // 3. Open the user's browser to the Twitch authorization page.
            string scopes = "chat:read channel:read:subscriptions user:read:follows user:read:email"; // Removed "channel:read:raids" as it's deprecated for this flow.
            string authUrl = $"https://id.twitch.tv/oauth2/authorize?client_id={ClientId}&redirect_uri={Uri.EscapeDataString(RedirectUri)}&response_type=code&scope={Uri.EscapeDataString(scopes)}";
            Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

            // 4. Wait for Twitch to redirect the user back to our local listener.
            // Add a timeout to prevent the application from hanging indefinitely.
            var listenerTask = listener.GetContextAsync();
            if (await Task.WhenAny(listenerTask, Task.Delay(90000)) != listenerTask)
            {
                // Timeout occurred
                listener.Stop();
                string errorMessage = "The login process timed out because the application did not receive a response from Twitch.\n\n" +
                                      "This usually means the 'OAuth Redirect URL' in your Twitch application settings is incorrect.\n\n" +
                                      $"Please ensure it is set to exactly: {RedirectUri}\n\n" +
                                      "A firewall may also be blocking the connection.";

                MessageBox.Show(errorMessage, "Login Timeout", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            // If we get here, the listener received a request.
            var context = await listenerTask;

            var request = context.Request;
            var response = context.Response;
            string responseString;

            // Check for an error from Twitch first.
            string? error = request.QueryString.Get("error");
            if (!string.IsNullOrEmpty(error))
            {
                string? errorDescription = request.QueryString.Get("error_description");
                responseString = $"<html><body><h2>Authentication Failed</h2><p>Twitch returned an error: <strong>{error}</strong></p><p>{errorDescription}</p><p>You can close this window.</p></body></html>";
                await SendResponseAndClose(response, responseString);
                listener.Stop();
                return null; // Return null to indicate failure.
            }

            string? code = request.QueryString.Get("code");

            // 5. Send a response to the browser to close the tab.
            responseString = "<html><body><h2>Authentication successful!</h2><p>You can close this window and return to the application.</p></body></html>";
            var buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            var output = response.OutputStream;
            await output.WriteAsync(buffer, 0, buffer.Length);
            output.Close();
            listener.Stop();

            return code;
        }

        private static async Task SendResponseAndClose(HttpListenerResponse response, string responseString)
        {
            var buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            var output = response.OutputStream;
            await output.WriteAsync(buffer, 0, buffer.Length);
            output.Close();
        }

        private static async Task<bool> ExchangeCodeForTokenAsync(string authCode)
        {
            using var client = new HttpClient();
            string tokenUrl = "https://id.twitch.tv/oauth2/token";

            var requestParams = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("client_id", ClientId),
                new KeyValuePair<string, string>("code", authCode),
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("redirect_uri", RedirectUri)
            };

            // The Client Secret is required for the token exchange when using your own registered application.
            requestParams.Add(new KeyValuePair<string, string>("client_secret", AppConfiguration.TwitchClientSecret));

            var content = new FormUrlEncodedContent(requestParams);

            var response = await client.PostAsync(tokenUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"[TwitchAuthService] Token exchange failed: {error}");
                return false;
            }

            var tokenData = await response.Content.ReadFromJsonAsync<TwitchTokenResponse>();
            if (tokenData == null || string.IsNullOrEmpty(tokenData.AccessToken))
            {
                return false;
            }

            // 6. Get user info with the new token.
            string? username = await GetTwitchUsername(tokenData.AccessToken);
            if (string.IsNullOrEmpty(username))
            {
                return false;
            }

            // 7. Save the tokens and username to configuration.
            AppConfiguration.TwitchUsername = username;
            AppConfiguration.TwitchChannelName = username; // Automatically set the channel to the user's own channel.
            AppConfiguration.TwitchOAuthToken = $"oauth:{tokenData.AccessToken}"; // The 'oauth:' prefix is required for IRC chat.
            AppConfiguration.TwitchRefreshToken = tokenData.RefreshToken ?? string.Empty;
            AppConfiguration.Save();

            return true;
        }

        private static async Task<string?> GetTwitchUsername(string accessToken)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            client.DefaultRequestHeaders.Add("Client-Id", ClientId);

            var response = await client.GetAsync("https://api.twitch.tv/helix/users");
            if (!response.IsSuccessStatusCode) return null;

            var userData = await response.Content.ReadFromJsonAsync<TwitchUsersResponse>();
            return userData?.Data?.FirstOrDefault()?.Login;
        }

        // Helper classes for deserializing Twitch's JSON responses.
        private class TwitchTokenResponse
        {
            [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
            [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
        }

        private class TwitchUsersResponse
        {
            [JsonPropertyName("data")] public TwitchUser[]? Data { get; set; }
        }

        private class TwitchUser
        {
            [JsonPropertyName("login")] public string? Login { get; set; }
        }
    }
}