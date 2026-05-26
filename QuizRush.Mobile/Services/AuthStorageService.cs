using Microsoft.Maui.Storage;

namespace QuizRush.Mobile.Services;

public class AuthStorageService
{
    private const string TokenKey = "auth_token";
    private const string UsernameKey = "auth_username";
    private const string EmailKey = "auth_email";

    public async Task SaveSessionAsync(string token, string username, string email)
    {
        await SetValueAsync(TokenKey, token);
        await SetValueAsync(UsernameKey, username);
        await SetValueAsync(EmailKey, email);
    }

    public Task<string?> GetTokenAsync() => GetValueAsync(TokenKey);

    public Task<string?> GetUsernameAsync() => GetValueAsync(UsernameKey);

    public Task<string?> GetEmailAsync() => GetValueAsync(EmailKey);

    public async Task ClearAsync()
    {
        await RemoveValueAsync(TokenKey);
        await RemoveValueAsync(UsernameKey);
        await RemoveValueAsync(EmailKey);
    }

    private static async Task SetValueAsync(string key, string value)
    {
        try
        {
            await SecureStorage.Default.SetAsync(key, value);
        }
        catch
        {
            Preferences.Default.Set(key, value);
        }
    }

    private static async Task<string?> GetValueAsync(string key)
    {
        try
        {
            return await SecureStorage.Default.GetAsync(key) ?? Preferences.Default.Get<string?>(key, null);
        }
        catch
        {
            return Preferences.Default.Get<string?>(key, null);
        }
    }

    private static Task RemoveValueAsync(string key)
    {
        try
        {
            SecureStorage.Default.Remove(key);
        }
        catch
        {
            // Ignore secure storage failures and fall back to preferences cleanup.
        }

        Preferences.Default.Remove(key);
        return Task.CompletedTask;
    }
}
