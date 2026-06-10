using QuizRush.Core.ViewModels;
using QuizRush.Mobile.Services;

namespace QuizRush.Mobile;

public partial class MainPage : ContentPage
{
    private readonly AuthService _authService;
    private readonly PlayerGameService _gameService;
    private readonly ApiEndpointProvider _endpointProvider;

    private string? _currentSessionCode;
    private bool _initialized;
    private bool _showLogin = true;

    public MainPage(AuthService authService, PlayerGameService gameService, ApiEndpointProvider endpointProvider)
    {
        InitializeComponent();

        _authService = authService;
        _gameService = gameService;
        _endpointProvider = endpointProvider;

        ApiBaseUrlLabel.Text = $"Backend: {_endpointProvider.GetBaseUrl()}";

        _authService.Session.SessionChanged += HandleSessionChanged;
        _gameService.GameJoined += HandleGameJoined;
        _gameService.GamblingPhaseStarted += HandleGamblingPhaseStarted;
        _gameService.QuestionReady += HandleQuestionReady;
        _gameService.AnswerRevealed += HandleAnswerRevealed;
        _gameService.ScoresUpdated += HandleScoresUpdated;
        _gameService.GameEnded += HandleGameEnded;
        _gameService.GameError += HandleGameError;
        _gameService.SubmissionPhaseEnded += HandleSubmissionPhaseEnded;
        _gameService.AllPlayersAnswered += HandleAllPlayersAnswered;
        _gameService.SessionExpired += HandleSessionExpired;

        UpdateAuthUi();
        UpdateAuthModeUi();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_initialized)
        {
            return;
        }

        _initialized = true;
        await _authService.InitializeAsync();
        UpdateAuthUi();
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        AuthStatusLabel.Text = string.Empty;

        if (string.IsNullOrWhiteSpace(LoginEmailEntry.Text) || string.IsNullOrWhiteSpace(LoginPasswordEntry.Text))
        {
            AuthStatusLabel.Text = "Enter both email and password.";
            return;
        }

        try
        {
            var auth = await _authService.LoginAsync(LoginEmailEntry.Text, LoginPasswordEntry.Text);
            PlayerNameEntry.Text = auth.Username;
            LoginPasswordEntry.Text = string.Empty;
            AuthStatusLabel.TextColor = Color.FromArgb("#047857");
            AuthStatusLabel.Text = "Logged in successfully.";
        }
        catch (Exception ex)
        {
            AuthStatusLabel.TextColor = Color.FromArgb("#B91C1C");
            AuthStatusLabel.Text = ToFriendlyAuthError(ex.Message);
        }
    }

    private async void OnRegisterClicked(object? sender, EventArgs e)
    {
        AuthStatusLabel.Text = string.Empty;

        if (string.IsNullOrWhiteSpace(RegisterUsernameEntry.Text)
            || string.IsNullOrWhiteSpace(RegisterEmailEntry.Text)
            || string.IsNullOrWhiteSpace(RegisterPasswordEntry.Text))
        {
            AuthStatusLabel.Text = "Fill in username, email, and password.";
            return;
        }

        try
        {
            await _authService.RegisterAsync(RegisterUsernameEntry.Text, RegisterEmailEntry.Text, RegisterPasswordEntry.Text);
            AuthStatusLabel.TextColor = Color.FromArgb("#047857");
            AuthStatusLabel.Text = "Account created. You can log in now.";
            LoginEmailEntry.Text = RegisterEmailEntry.Text;
            PlayerNameEntry.Text = RegisterUsernameEntry.Text;
            RegisterPasswordEntry.Text = string.Empty;
            ShowLoginMode();
        }
        catch (Exception ex)
        {
            AuthStatusLabel.TextColor = Color.FromArgb("#B91C1C");

            if (IsExistingAccountMessage(ex.Message))
            {
                AuthStatusLabel.Text = "This account already exists. Please log in.";
                LoginEmailEntry.Text = RegisterEmailEntry.Text;
                PlayerNameEntry.Text = RegisterUsernameEntry.Text;
                RegisterPasswordEntry.Text = string.Empty;
                ShowLoginMode();
                return;
            }

            AuthStatusLabel.Text = ToFriendlyAuthError(ex.Message);
        }
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        await _authService.LogoutAsync();
        AuthStatusLabel.TextColor = Color.FromArgb("#047857");
        AuthStatusLabel.Text = "Logged out.";
    }

    private void OnShowLoginClicked(object? sender, EventArgs e)
    {
        ShowLoginMode();
    }

    private void OnShowRegisterClicked(object? sender, EventArgs e)
    {
        ShowRegisterMode();
    }

    private async void OnJoinClicked(object? sender, EventArgs e)
    {
        JoinStatusLabel.TextColor = Color.FromArgb("#B91C1C");
        JoinStatusLabel.Text = string.Empty;

        var playerName = (PlayerNameEntry.Text ?? string.Empty).Trim();
        var sessionCode = (SessionCodeEntry.Text ?? string.Empty).Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(playerName) || string.IsNullOrWhiteSpace(sessionCode))
        {
            JoinStatusLabel.Text = "Enter both a player name and session PIN.";
            return;
        }

        JoinButton.IsEnabled = false;
        JoinButton.Text = "Joining...";

        try
        {
            _currentSessionCode = sessionCode;
            await _gameService.JoinGameAsync(sessionCode, playerName);
        }
        catch (Exception ex)
        {
            JoinStatusLabel.Text = ex.Message;
            JoinButton.IsEnabled = true;
            JoinButton.Text = "Join session";
        }
    }

    private async void OnSubmitGambleClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_currentSessionCode))
        {
            return;
        }

        if (!int.TryParse(GambleEntry.Text, out var percentage) || percentage < 0 || percentage > 100)
        {
            GameStateLabel.Text = "Enter a gamble percentage from 0 to 100.";
            return;
        }

        try
        {
            await _gameService.PlaceGambleAsync(_currentSessionCode, percentage);
            GameStateLabel.Text = $"Gamble locked at {percentage}% for this round.";
            GamblePanel.IsVisible = false;
        }
        catch (Exception ex)
        {
            GameStateLabel.Text = ex.Message;
        }
    }

    private async void OnLeaveClicked(object? sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_currentSessionCode))
        {
            await _gameService.LeaveGameAsync(_currentSessionCode);
        }

        ResetGameUi("You left the session.");
    }

    private void HandleSessionChanged()
    {
        MainThread.BeginInvokeOnMainThread(UpdateAuthUi);
    }

    private void HandleGameJoined(string sessionCode, int totalPlayers, string hostName)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _currentSessionCode = sessionCode;
            LiveSessionCodeLabel.Text = sessionCode;
            GamePanel.IsVisible = true;
            JoinButton.IsEnabled = true;
            JoinButton.Text = "Join session";
            JoinStatusLabel.TextColor = Color.FromArgb("#047857");
            JoinStatusLabel.Text = $"Joined {sessionCode}. Players in lobby: {totalPlayers}. Host: {hostName}.";
            GameStateLabel.Text = "Waiting for the host to start the game.";
            AnswerFeedbackLabel.Text = string.Empty;
            QuestionPanel.IsVisible = false;
            GamblePanel.IsVisible = false;
            AnswersLayout.Children.Clear();
        });
    }

    private void HandleGamblingPhaseStarted(int seconds)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            AnswerFeedbackLabel.Text = string.Empty;
            GambleEntry.Text = string.Empty;
            GamblePanel.IsVisible = seconds > 0;
            GamblePromptLabel.Text = seconds > 0
                ? $"Gambling phase: choose 0-100% within {seconds} seconds."
                : "First question is starting.";
            GameStateLabel.Text = seconds > 0
                ? "Lock your gamble before the next question opens."
                : "Question incoming.";
        });
    }

    private void HandleQuestionReady(QuestionData question, int timeLimit)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            QuestionPanel.IsVisible = true;
            QuestionTextLabel.Text = question.Text;
            QuestionMetaLabel.Text = $"{question.PointsValue} points - {timeLimit} seconds";
            GameStateLabel.Text = "Question live. Pick your answer.";
            AnswerFeedbackLabel.Text = string.Empty;
            AnswersLayout.Children.Clear();

            foreach (var answer in question.Answers)
            {
                var button = new Button
                {
                    Text = answer.Text,
                    CommandParameter = answer.AnswerId
                };
                button.Clicked += async (_, _) =>
                {
                    if (string.IsNullOrWhiteSpace(_currentSessionCode))
                    {
                        return;
                    }

                    try
                    {
                        await _gameService.SubmitAnswerAsync(_currentSessionCode, answer.AnswerId);
                        SetAnswerButtonsEnabled(false);
                        GameStateLabel.Text = "Answer submitted. Waiting for reveal.";
                    }
                    catch (Exception ex)
                    {
                        GameStateLabel.Text = ex.Message;
                    }
                };

                AnswersLayout.Children.Add(button);
            }
        });
    }

    private void HandleAnswerRevealed(AnswerData answer)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            AnswerFeedbackLabel.TextColor = answer.IsCorrect ? Color.FromArgb("#047857") : Color.FromArgb("#B91C1C");
            AnswerFeedbackLabel.Text = answer.IsCorrect
                ? $"Correct. +{answer.TotalPoints} points."
                : $"Wrong answer. {answer.TotalPoints} points this round.";
        });
    }

    private void HandleScoresUpdated(LeaderboardData[] leaderboard)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            LeaderboardView.ItemsSource = leaderboard;
        });
    }

    private void HandleGameEnded(LeaderboardData[] leaderboard)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            LeaderboardView.ItemsSource = leaderboard;
            GameStateLabel.Text = "Game finished.";
            QuestionPanel.IsVisible = false;
            GamblePanel.IsVisible = false;
            SetAnswerButtonsEnabled(false);
        });
    }

    private void HandleGameError(string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            JoinButton.IsEnabled = true;
            JoinButton.Text = "Join session";
            JoinStatusLabel.TextColor = Color.FromArgb("#B91C1C");
            JoinStatusLabel.Text = message;
            GameStateLabel.Text = message;
        });
    }

    private void HandleSubmissionPhaseEnded()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            SetAnswerButtonsEnabled(false);
            GameStateLabel.Text = "Submission closed. Revealing results.";
        });
    }

    private void HandleAllPlayersAnswered()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            GameStateLabel.Text = "All players answered.";
        });
    }

    private void HandleSessionExpired()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ResetGameUi("Session expired or host disconnected.");
        });
    }

    private void UpdateAuthUi()
    {
        var session = _authService.Session;
        LoggedOutPanel.IsVisible = !session.IsAuthenticated;
        LoggedInPanel.IsVisible = session.IsAuthenticated;

        if (session.IsAuthenticated)
        {
            WelcomeLabel.Text = $"Signed in as {session.Username}";
            EmailLabel.Text = session.Email ?? string.Empty;
            if (string.IsNullOrWhiteSpace(PlayerNameEntry.Text))
            {
                PlayerNameEntry.Text = session.Username;
            }
        }

        UpdateAuthModeUi();
    }

    private void ResetGameUi(string statusMessage)
    {
        _currentSessionCode = null;
        GamePanel.IsVisible = false;
        JoinButton.IsEnabled = true;
        JoinButton.Text = "Join session";
        JoinStatusLabel.TextColor = Color.FromArgb("#4B5563");
        JoinStatusLabel.Text = statusMessage;
        GameStateLabel.Text = statusMessage;
        QuestionPanel.IsVisible = false;
        GamblePanel.IsVisible = false;
        AnswersLayout.Children.Clear();
        LeaderboardView.ItemsSource = null;
        AnswerFeedbackLabel.Text = string.Empty;
    }

    private void SetAnswerButtonsEnabled(bool enabled)
    {
        foreach (var child in AnswersLayout.Children.OfType<Button>())
        {
            child.IsEnabled = enabled;
        }
    }

    private void ShowLoginMode()
    {
        _showLogin = true;
        AuthStatusLabel.Text = string.Empty;
        UpdateAuthModeUi();
    }

    private static bool IsExistingAccountMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        return message.Contains("already exists", StringComparison.OrdinalIgnoreCase)
            || message.Contains("already in use", StringComparison.OrdinalIgnoreCase)
            || message.Contains("already taken", StringComparison.OrdinalIgnoreCase);
    }

    private static string ToFriendlyAuthError(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "Something went wrong. Try again.";
        }

        if (message.Contains("Cannot open database", StringComparison.OrdinalIgnoreCase)
            || message.Contains("transient failure", StringComparison.OrdinalIgnoreCase)
            || message.Contains("SqlException", StringComparison.OrdinalIgnoreCase))
        {
            return "Backend database is not ready. Start QuizRush.Api in Visual Studio, then try again.";
        }

        if (message.Contains("Connection refused", StringComparison.OrdinalIgnoreCase)
            || message.Contains("No connection could be made", StringComparison.OrdinalIgnoreCase))
        {
            return "Cannot reach the API. Start QuizRush.Api (http://localhost:5176) and try again.";
        }

        return message.Length > 200 ? $"{message[..200]}…" : message;
    }

    private void ShowRegisterMode()
    {
        _showLogin = false;
        AuthStatusLabel.Text = string.Empty;
        UpdateAuthModeUi();
    }

    private void UpdateAuthModeUi()
    {
        LoginFormPanel.IsVisible = _showLogin;
        RegisterFormPanel.IsVisible = !_showLogin;

        LoginModeButton.BackgroundColor = _showLogin ? Color.FromArgb("#233876") : Color.FromArgb("#E5E7EB");
        LoginModeButton.TextColor = _showLogin ? Colors.White : Color.FromArgb("#344054");

        RegisterModeButton.BackgroundColor = !_showLogin ? Color.FromArgb("#7C3AED") : Color.FromArgb("#E5E7EB");
        RegisterModeButton.TextColor = !_showLogin ? Colors.White : Color.FromArgb("#344054");
    }
}
