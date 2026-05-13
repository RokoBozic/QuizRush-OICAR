using QuizRush.Core.ViewModels;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace QuizRush.Web.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly LocalStorageService _localStorage;
        private const string TokenKey = "token";

        public event Action? OnAuthStateChanged;

        public AuthService(HttpClient httpClient, LocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;
        }

        private void RaiseStateChanged() => OnAuthStateChanged?.Invoke();

        public async Task<AuthResponseViewModel?> LoginAsync(string email, string password)
        {
            var payload = new LoginViewModel
            {
                Email = email,
                Password = password
            };

            var response = await _httpClient.PostAsJsonAsync("api/auth/login", payload);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var auth = await response.Content.ReadFromJsonAsync<AuthResponseViewModel>();
            if (auth != null && !string.IsNullOrWhiteSpace(auth.Token))
            {
                await SaveTokenAsync(auth.Token);
                RaiseStateChanged();
            }

            return auth;
        }

        public async Task<string?> RegisterAsync(string username, string email, string password)
        {
            var payload = new RegisterViewModel
            {
                Username = username,
                Email = email,
                Password = password
            };

            var response = await _httpClient.PostAsJsonAsync("api/auth/register", payload);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadAsStringAsync();
        }

        public Task<string?> GetStoredTokenAsync()
        {
            return _localStorage.GetItemAsync(TokenKey);
        }

        public async Task<string?> GetValidStoredTokenAsync()
        {
            var token = await GetStoredTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            if (IsTokenExpired(token))
            {
                await ClearTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = null;
                RaiseStateChanged();
                return null;
            }

            return token;
        }

        public Task SaveTokenAsync(string token)
        {
            return _localStorage.SetItemAsync(TokenKey, token);
        }

        public Task ClearTokenAsync()
        {
            return _localStorage.RemoveItemAsync(TokenKey);
        }

        public async Task LogoutAsync()
        {
            await ClearTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = null;
            RaiseStateChanged();
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            var token = await GetValidStoredTokenAsync();
            return !string.IsNullOrWhiteSpace(token);
        }

        public async Task ApplyAuthorizationHeaderAsync(HttpClient client)
        {
            var token = await GetValidStoredTokenAsync();
            client.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(token)
                ? null
                : new AuthenticationHeaderValue("Bearer", token);
        }

        private static bool IsTokenExpired(string token)
        {
            try
            {
                var parts = token.Split('.');
                if (parts.Length < 2)
                {
                    return true;
                }

                var payload = parts[1]
                    .Replace('-', '+')
                    .Replace('_', '/');
                payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

                var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                using var document = JsonDocument.Parse(json);
                if (!document.RootElement.TryGetProperty("exp", out var exp))
                {
                    return true;
                }

                var expiry = DateTimeOffset.FromUnixTimeSeconds(exp.GetInt64());
                return expiry <= DateTimeOffset.UtcNow;
            }
            catch
            {
                return true;
            }
        }
    }
}
