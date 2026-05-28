using QuizRush.Core.ViewModels;
using QuizRush.Mobile.Services;

namespace QuizRush.Mobile.Pages;

public partial class AccountPage : ContentPage
{
    private readonly AuthService _authService;
    private readonly UserApiService _userApiService;

    public AccountPage(AuthService authService, UserApiService userApiService)
    {
        InitializeComponent();
        _authService = authService;
        _userApiService = userApiService;
        _authService.Session.SessionChanged += HandleSessionChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefreshAsync();
    }

    private async void OnSaveProfileClicked(object? sender, EventArgs e)
    {
        try
        {
            await _userApiService.UpdateProfileAsync(new UpdateProfileViewModel
            {
                Username = UsernameEntry.Text?.Trim() ?? string.Empty,
                Email = EmailEntry.Text?.Trim() ?? string.Empty
            });

            AccountStatusLabel.Text = "Profile updated.";
        }
        catch (Exception ex)
        {
            AccountStatusLabel.Text = ex.Message;
        }
    }

    private async void OnChangePasswordClicked(object? sender, EventArgs e)
    {
        try
        {
            await _userApiService.ChangePasswordAsync(new ChangePasswordViewModel
            {
                CurrentPassword = CurrentPasswordEntry.Text ?? string.Empty,
                NewPassword = NewPasswordEntry.Text ?? string.Empty
            });

            CurrentPasswordEntry.Text = string.Empty;
            NewPasswordEntry.Text = string.Empty;
            AccountStatusLabel.Text = "Password changed.";
        }
        catch (Exception ex)
        {
            AccountStatusLabel.Text = ex.Message;
        }
    }

    private async Task RefreshAsync()
    {
        var isAuthenticated = _authService.Session.IsAuthenticated;
        AccountLoggedOutPanel.IsVisible = !isAuthenticated;
        AccountLoggedInPanel.IsVisible = isAuthenticated;

        if (!isAuthenticated)
        {
            AccountStatusLabel.Text = "Log in to view your account.";
            return;
        }

        try
        {
            var profile = await _userApiService.GetProfileAsync();
            var stats = await _userApiService.GetStatsAsync();

            if (profile is not null)
            {
                UsernameEntry.Text = profile.Username;
                EmailEntry.Text = profile.Email;
            }

            if (stats is not null)
            {
                TotalPointsLabel.Text = stats.AccumulatedPoints.ToString();
                GamesPlayedLabel.Text = stats.GamesPlayed.ToString();
                GamesWonLabel.Text = stats.GamesWon.ToString();
                HighestScoreLabel.Text = stats.HighestScore.ToString();
                HistoryView.ItemsSource = stats.GameHistory;
            }

            AccountStatusLabel.Text = "Profile loaded.";
        }
        catch (Exception ex)
        {
            AccountStatusLabel.Text = ex.Message;
        }
    }

    private void HandleSessionChanged()
    {
        MainThread.BeginInvokeOnMainThread(async () => await RefreshAsync());
    }
}
