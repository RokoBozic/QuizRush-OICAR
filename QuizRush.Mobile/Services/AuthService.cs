using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using QuizRush.Core.ViewModels;

namespace QuizRush.Mobile.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly AuthStorageService _storageService;
    private readonly AppSession _session;

    public AuthService(HttpClient httpClient, AuthStorageService storageService, AppSession session)
    {
        _httpClient = httpClient;
        _storageService = storageService;
        _session = session;
    }

    public AppSession Session => _session;

    public async Task InitializeAsync()
    {
        var token = await _storageService.GetTokenAsync();
        if (string.IsNullOrWhiteSpace(token) || IsTokenExpired(token))
        {
            await LogoutAsync();
            return;
        }

        _session.Restore(
            token,
            await _storageService.GetUsernameAsync(),
            await _storageService.GetEmailAsync());

        ApplyAuthorizationHeader();
    }

    public async Task<AuthResponseViewModel> LoginAsync(string email, string password)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", new LoginViewModel
        {
            Email = email.Trim(),
            Password = password
        });

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await ReadErrorAsync(response, "Login failed."));
        }

        var auth = await response.Content.ReadFromJsonAsync<AuthResponseViewModel>();
        if (auth is null || string.IsNullOrWhiteSpace(auth.Token))
        {
            throw new InvalidOperationException("The server returned an invalid login response.");
        }

        await _storageService.SaveSessionAsync(auth.Token, auth.Username, auth.Email);
        _session.Set(auth);
        ApplyAuthorizationHeader();
        return auth;
    }

    public async Task RegisterAsync(string username, string email, string password)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/register", new RegisterViewModel
        {
            Username = username.Trim(),
            Email = email.Trim(),
            Password = password
        });

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await ReadErrorAsync(response, "Registration failed."));
        }
    }

    public async Task LogoutAsync()
    {
        await _storageService.ClearAsync();
        _httpClient.DefaultRequestHeaders.Authorization = null;
        _session.Clear();
    }

    public string? GetAccessToken()
    {
        return _session.IsAuthenticated && !string.IsNullOrWhiteSpace(_session.Token)
            ? _session.Token
            : null;
    }

    public void ApplyAuthorizationHeader()
    {
        _httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(_session.Token)
            ? null
            : new AuthenticationHeaderValue("Bearer", _session.Token);
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, string fallback)
    {
        var raw = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return fallback;
        }

        if (raw.TrimStart().StartsWith('{'))
        {
            try
            {
                using var document = JsonDocument.Parse(raw);
                if (document.RootElement.TryGetProperty("message", out var message))
                {
                    return message.GetString() ?? fallback;
                }
            }
            catch
            {
                // Fall through to plain-text handling.
            }
        }

        var trimmed = raw.Trim('"');
        if (trimmed.Contains("<html", StringComparison.OrdinalIgnoreCase))
        {
            return fallback;
        }

        return trimmed.Length > 300 ? $"{trimmed[..300]}…" : trimmed;
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

            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var document = JsonDocument.Parse(json);

            if (!document.RootElement.TryGetProperty("exp", out var expiry))
            {
                return true;
            }

            return DateTimeOffset.FromUnixTimeSeconds(expiry.GetInt64()) <= DateTimeOffset.UtcNow;
        }
        catch
        {
            return true;
        }
    }
}
