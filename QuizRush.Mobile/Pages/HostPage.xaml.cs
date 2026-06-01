using QuizRush.Core.ViewModels;
using QuizRush.Mobile.Services;

namespace QuizRush.Mobile.Pages;

public partial class HostPage : ContentPage
{
    private readonly AuthService _authService;
    private readonly QuizApiService _quizApiService;
    private readonly PlayerGameService _gameService;

    private List<QuizResponseViewModel> _quizzes = [];
    private string? _currentSessionCode;
    private int _playerCount;

    public HostPage(AuthService authService, QuizApiService quizApiService, PlayerGameService gameService)
    {
        InitializeComponent();
        _authService = authService;
        _quizApiService = quizApiService;
        _gameService = gameService;

        _authService.Session.SessionChanged += HandleSessionChanged;
        _gameService.GameJoined += HandleGameJoined;
        _gameService.PlayerJoined += HandlePlayerJoined;
        _gameService.GameStarted += HandleGameStarted;
        _gameService.QuestionReady += HandleQuestionReady;
        _gameService.QuestionAnswered += HandleQuestionAnswered;
        _gameService.HostSelfAck += HandleHostNotice;
        _gameService.GameEnded += HandleGameEnded;
        _gameService.GameError += HandleGameError;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefreshAsync();
    }

    private async void OnHostSelectedQuizClicked(object? sender, EventArgs e)
    {
        if (QuizPicker.SelectedIndex < 0 || QuizPicker.SelectedIndex >= _quizzes.Count)
        {
            HostStatusLabel.Text = "Pick a quiz first.";
            return;
        }

        try
        {
            var quiz = _quizzes[QuizPicker.SelectedIndex];
            await _gameService.HostGameAsync(quiz.Id);
        }
        catch (Exception ex)
        {
            HostStatusLabel.Text = ex.Message;
        }
    }

    private async void OnStartGameClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_currentSessionCode))
        {
            return;
        }

        await _gameService.StartGameAsync(_currentSessionCode);
    }

    private async void OnNextQuestionClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_currentSessionCode))
        {
            return;
        }

        await _gameService.NextQuestionAsync(_currentSessionCode);
    }

    private async void OnEndGameClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_currentSessionCode))
        {
            return;
        }

        await _gameService.EndGameAsync(_currentSessionCode);
    }

    private async void OnEditQuizClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is long quizId)
        {
            await DisplayAlert("Use Quizzes Tab", $"Open the Quizzes tab to edit quiz #{quizId}.", "OK");
        }
        else if (sender is Button stringButton && long.TryParse(stringButton.CommandParameter?.ToString(), out var parsedId))
        {
            await DisplayAlert("Use Quizzes Tab", $"Open the Quizzes tab to edit quiz #{parsedId}.", "OK");
        }
    }

    private async Task RefreshAsync()
    {
        var isAuthenticated = _authService.Session.IsAuthenticated;
        HostLoggedOutPanel.IsVisible = !isAuthenticated;
        HostLoggedInPanel.IsVisible = isAuthenticated;

        if (!isAuthenticated)
        {
            HostStatusLabel.Text = "Log in to host quizzes.";
            return;
        }

        try
        {
            _quizzes = await _quizApiService.GetMyQuizzesAsync();
            QuizPicker.ItemsSource = _quizzes.Select(q => q.Title).ToList();
            QuizListView.ItemsSource = _quizzes;
            QuizCountLabel.Text = _quizzes.Count.ToString();
            QuestionCountLabel.Text = _quizzes.Sum(q => q.Questions.Count).ToString();
            HostStatusLabel.Text = "Ready to host.";
        }
        catch (Exception ex)
        {
            HostStatusLabel.Text = ex.Message;
        }
    }

    private void HandleSessionChanged()
    {
        MainThread.BeginInvokeOnMainThread(async () => await RefreshAsync());
    }

    private void HandleGameJoined(string sessionCode, int totalPlayers, string hostName)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _currentSessionCode = sessionCode;
            _playerCount = totalPlayers;
            SessionPanel.IsVisible = true;
            SessionCodeLabel.Text = $"PIN: {sessionCode}";
            SessionStateLabel.Text = $"Host: {hostName}";
            PlayersLabel.Text = $"Players joined: {totalPlayers}";
            HostQuestionLabel.Text = "Waiting to start...";
            HostAnswerCountLabel.Text = string.Empty;
            StartGameButton.IsVisible = true;
            NextQuestionButton.IsVisible = false;
            EndGameButton.IsVisible = true;
        });
    }

    private void HandlePlayerJoined(string playerName, int totalPlayers)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _playerCount = totalPlayers;
            PlayersLabel.Text = $"Players joined: {totalPlayers}";
            SessionStateLabel.Text = $"{playerName} joined the lobby.";
        });
    }

    private void HandleGameStarted(int questionCount)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            SessionStateLabel.Text = $"Game live - {questionCount} question(s).";
            StartGameButton.IsVisible = false;
            NextQuestionButton.IsVisible = true;
            EndGameButton.IsVisible = true;
        });
    }

    private void HandleQuestionReady(QuestionData question, int timeLimit)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            HostQuestionLabel.Text = question.Text;
            HostAnswerCountLabel.Text = $"Question timer: {timeLimit}s";
        });
    }

    private void HandleQuestionAnswered(int answered, int total)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            HostAnswerCountLabel.Text = $"Answers submitted: {answered}/{total}";
        });
    }

    private void HandleHostNotice(string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            SessionStateLabel.Text = message;
        });
    }

    private void HandleGameEnded(LeaderboardData[] leaderboard)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            SessionStateLabel.Text = "Game finished.";
            HostQuestionLabel.Text = leaderboard.Length > 0
                ? $"Winner: {leaderboard[0].PlayerName} ({leaderboard[0].Score})"
                : "No results.";
            HostAnswerCountLabel.Text = string.Empty;
            StartGameButton.IsVisible = true;
            NextQuestionButton.IsVisible = false;
            EndGameButton.IsVisible = false;
            _currentSessionCode = null;
        });
    }

    private void HandleGameError(string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            HostStatusLabel.Text = message;
        });
    }
}
