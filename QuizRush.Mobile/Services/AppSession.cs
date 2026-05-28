using QuizRush.Core.ViewModels;

namespace QuizRush.Mobile.Services;

public class AppSession
{
    public string? Token { get; private set; }
    public string? Username { get; private set; }
    public string? Email { get; private set; }

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Token);

    public event Action? SessionChanged;

    public void Set(AuthResponseViewModel auth)
    {
        Token = auth.Token;
        Username = auth.Username;
        Email = auth.Email;
        SessionChanged?.Invoke();
    }

    public void Restore(string token, string? username, string? email)
    {
        Token = token;
        Username = username;
        Email = email;
        SessionChanged?.Invoke();
    }

    public void UpdateIdentity(string username, string email)
    {
        Username = username;
        Email = email;
        SessionChanged?.Invoke();
    }

    public void Clear()
    {
        Token = null;
        Username = null;
        Email = null;
        SessionChanged?.Invoke();
    }
}
