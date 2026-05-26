using System.Net.Http.Json;
using QuizRush.Core.ViewModels;

namespace QuizRush.Mobile.Services;

public class UserApiService
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;

    public UserApiService(HttpClient httpClient, AuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
    }

    public async Task<UserProfileViewModel?> GetProfileAsync()
    {
        _authService.ApplyAuthorizationHeader();
        return await _httpClient.GetFromJsonAsync<UserProfileViewModel>("api/user/profile");
    }

    public async Task<PlayerStatsViewModel?> GetStatsAsync()
    {
        _authService.ApplyAuthorizationHeader();
        return await _httpClient.GetFromJsonAsync<PlayerStatsViewModel>("api/user/stats");
    }

    public async Task UpdateProfileAsync(UpdateProfileViewModel model)
    {
        _authService.ApplyAuthorizationHeader();
        using var response = await _httpClient.PutAsJsonAsync("api/user/profile", model);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await ReadErrorAsync(response, "Could not update profile."));
        }

        _authService.Session.UpdateIdentity(model.Username, model.Email);
    }

    public async Task ChangePasswordAsync(ChangePasswordViewModel model)
    {
        _authService.ApplyAuthorizationHeader();
        using var response = await _httpClient.PutAsJsonAsync("api/user/change-password", model);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await ReadErrorAsync(response, "Could not change password."));
        }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, string fallback)
    {
        var raw = await response.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(raw) ? fallback : raw.Trim('"');
    }
}
