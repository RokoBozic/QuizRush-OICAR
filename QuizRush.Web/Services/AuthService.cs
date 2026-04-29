using QuizRush.Core.ViewModels;
using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace QuizRush.Web.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly LocalStorageService _localStorage;
        private const string TokenKey = "token";

        public AuthService(HttpClient httpClient, LocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;
        }

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

        public Task SaveTokenAsync(string token)
        {
            return _localStorage.SetItemAsync(TokenKey, token);
        }

        public Task ClearTokenAsync()
        {
            return _localStorage.RemoveItemAsync(TokenKey);
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            var token = await GetStoredTokenAsync();
            return !string.IsNullOrWhiteSpace(token);
        }

        public async Task ApplyAuthorizationHeaderAsync(HttpClient client)
        {
            var token = await GetStoredTokenAsync();
            client.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(token)
                ? null
                : new AuthenticationHeaderValue("Bearer", token);
        }
    }
}
