using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using QuizRush.Core.ViewModels;

namespace QuizRush.Mobile.Services;

public class PlayerGameService : IAsyncDisposable
{
    private readonly ApiEndpointProvider _endpointProvider;
    private readonly AuthService _authService;
    private HubConnection? _hubConnection;

    public event Action<string, int, string>? GameJoined;
    public event Action<string, int>? PlayerJoined;
    public event Action<int>? GameStarted;
    public event Action<int, int>? QuestionAnswered;
    public event Action<int, int>? QuestionDisplayed;
    public event Action<string>? HostSelfAck;
    public event Action<string>? HostPlayerNotice;
    public event Action<int>? GamblingPhaseStarted;
    public event Action<QuestionData, int>? QuestionReady;
    public event Action<AnswerData>? AnswerRevealed;
    public event Action<LeaderboardData[]>? ScoresUpdated;
    public event Action<LeaderboardData[]>? GameEnded;
    public event Action<string>? GameError;
    public event Action? SubmissionPhaseEnded;
    public event Action? AllPlayersAnswered;
    public event Action? SessionExpired;

    public async Task HostGameAsync(long quizId)
    {
        await EnsureConnectedAsync();
        await _hubConnection!.InvokeAsync("HostGame", quizId);
    }

    public async Task RejoinGameAsync(string sessionCode)
    {
        await EnsureConnectedAsync();
        await _hubConnection!.InvokeAsync("RejoinGame", sessionCode.Trim().ToUpperInvariant());
    }

    public async Task JoinGameAsync(string sessionCode, string playerName)
    {
        await EnsureConnectedAsync();
        await _hubConnection!.InvokeAsync("JoinGame", sessionCode.Trim().ToUpperInvariant(), playerName.Trim());
    }

    public async Task StartGameAsync(string sessionCode)
    {
        await EnsureConnectedAsync();
        await _hubConnection!.InvokeAsync("StartGame", sessionCode.Trim().ToUpperInvariant());
    }

    public async Task NextQuestionAsync(string sessionCode)
    {
        await EnsureConnectedAsync();
        await _hubConnection!.InvokeAsync("NextQuestion", sessionCode.Trim().ToUpperInvariant());
    }

    public async Task EndGameAsync(string sessionCode)
    {
        await EnsureConnectedAsync();
        await _hubConnection!.InvokeAsync("EndGame", sessionCode.Trim().ToUpperInvariant());
    }

    public async Task SubmitAnswerAsync(string sessionCode, long answerId)
    {
        await EnsureConnectedAsync();
        await _hubConnection!.InvokeAsync("SubmitAnswer", sessionCode.Trim().ToUpperInvariant(), answerId);
    }

    public async Task PlaceGambleAsync(string sessionCode, int percentage)
    {
        await EnsureConnectedAsync();
        await _hubConnection!.InvokeAsync("PlaceGamble", sessionCode.Trim().ToUpperInvariant(), percentage);
    }

    public async Task LeaveGameAsync(string sessionCode)
    {
        if (_hubConnection is null)
        {
            return;
        }

        if (_hubConnection.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("LeaveGame", sessionCode.Trim().ToUpperInvariant());
            await _hubConnection.StopAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }

    private async Task EnsureConnectedAsync()
    {
        if (_hubConnection is null)
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_endpointProvider.GetHubUrl(), options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(_authService.GetAccessToken());
                })
                .AddJsonProtocol(o =>
                {
                    o.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    o.PayloadSerializerOptions.PropertyNameCaseInsensitive = true;
                })
                .WithAutomaticReconnect()
                .Build();

            ConfigureEvents(_hubConnection);
        }

        if (_hubConnection.State == HubConnectionState.Disconnected)
        {
            await _hubConnection.StartAsync();
        }
    }

    private void ConfigureEvents(HubConnection hub)
    {
        hub.On<string, int, string>("GameJoined", (sessionCode, totalPlayers, hostName) =>
        {
            GameJoined?.Invoke(sessionCode, totalPlayers, hostName);
        });

        hub.On<string, int>("PlayerJoined", (playerName, totalPlayers) =>
        {
            PlayerJoined?.Invoke(playerName, totalPlayers);
        });

        hub.On<int>("GameStarted", questionCount =>
        {
            GameStarted?.Invoke(questionCount);
        });

        hub.On<int, int>("QuestionAnswered", (answered, total) =>
        {
            QuestionAnswered?.Invoke(answered, total);
        });

        hub.On<int, int>("QuestionDisplayed", (questionNumber, timeLimit) =>
        {
            QuestionDisplayed?.Invoke(questionNumber, timeLimit);
        });

        hub.On<string>("HostSelfAck", message =>
        {
            HostSelfAck?.Invoke(message);
        });

        hub.On<string>("HostPlayerNotice", message =>
        {
            HostPlayerNotice?.Invoke(message);
        });

        hub.On<int>("GamblingPhaseStarted", seconds =>
        {
            GamblingPhaseStarted?.Invoke(seconds);
        });

        hub.On<QuestionData, int>("QuestionReady", (question, timeLimit) =>
        {
            QuestionReady?.Invoke(question, timeLimit);
        });

        hub.On<AnswerData>("AnswerRevealed", answer =>
        {
            AnswerRevealed?.Invoke(answer);
        });

        hub.On<LeaderboardData[]>("ScoresUpdated", leaderboard =>
        {
            ScoresUpdated?.Invoke(leaderboard);
        });

        hub.On<LeaderboardData[]>("GameEnded", leaderboard =>
        {
            GameEnded?.Invoke(leaderboard);
        });

        hub.On<string>("GameError", message =>
        {
            GameError?.Invoke(message);
        });

        hub.On("SubmissionPhaseEnded", () =>
        {
            SubmissionPhaseEnded?.Invoke();
        });

        hub.On("AllPlayersAnswered", () =>
        {
            AllPlayersAnswered?.Invoke();
        });

        hub.On("SessionExpired", () =>
        {
            SessionExpired?.Invoke();
        });
    }

    public PlayerGameService(ApiEndpointProvider endpointProvider, AuthService authService)
    {
        _endpointProvider = endpointProvider;
        _authService = authService;
    }
}
