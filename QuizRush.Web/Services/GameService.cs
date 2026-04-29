using Microsoft.AspNetCore.SignalR.Client;
using QuizRush.Core.ViewModels;

namespace QuizRush.Web.Services
{
    public class GameService
    {
        private readonly AuthService _authService;
        private HubConnection? _hubConnection;

        public event Action<string, int>? OnPlayerJoined;
        public event Action<QuestionData, int>? OnQuestionReady;
        public event Action<AnswerData>? OnAnswerRevealed;
        public event Action<LeaderboardData[]>? OnScoresUpdated;
        public event Action<LeaderboardData[]>? OnGameEnded;
        public event Action<string>? OnGameError;

        public event Action<string, int, string>? OnGameJoined;

        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
        private HubConnection Hub => _hubConnection ?? throw new InvalidOperationException("Hub connection not initialized.");

        public GameService(AuthService authService)
        {
            _authService = authService;
        }

        public async Task EnsureConnectedAsync()
        {
            if (_hubConnection == null)
            {
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl("http://localhost:5176/hub/game", options =>
                    {
                        options.AccessTokenProvider = async () => await _authService.GetStoredTokenAsync();
                    })
                    .WithAutomaticReconnect()
                    .Build();

                ConfigureServerEvents();
            }

            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                await _hubConnection.StartAsync();
            }
        }

        public async Task HostGame(long quizId)
        {
            await EnsureConnectedAsync();
            await Hub.InvokeAsync("HostGame", quizId);
        }

        public async Task JoinGame(string sessionCode, string playerName)
        {
            await EnsureConnectedAsync();
            await Hub.InvokeAsync("JoinGame", sessionCode, playerName);
        }

        public async Task StartGame(string sessionCode)
        {
            await EnsureConnectedAsync();
            await Hub.InvokeAsync("StartGame", sessionCode);
        }

        public async Task NextQuestion(string sessionCode)
        {
            await EnsureConnectedAsync();
            await Hub.InvokeAsync("NextQuestion", sessionCode);
        }

        public async Task SubmitAnswer(string sessionCode, long answerId)
        {
            await EnsureConnectedAsync();
            await Hub.InvokeAsync("SubmitAnswer", sessionCode, answerId);
        }

        public async Task PlaceGamble(string sessionCode, int percentage)
        {
            await EnsureConnectedAsync();
            await Hub.InvokeAsync("PlaceGamble", sessionCode, percentage);
        }

        public async Task EndGame(string sessionCode)
        {
            await EnsureConnectedAsync();
            await Hub.InvokeAsync("EndGame", sessionCode);
        }

        private void ConfigureServerEvents()
        {
            Hub.On<string, int>("PlayerJoined", (name, totalPlayers) =>
            {
                OnPlayerJoined?.Invoke(name, totalPlayers);
            });

            Hub.On<QuestionData, int>("QuestionReady", (question, timeLimit) =>
            {
                OnQuestionReady?.Invoke(question, timeLimit);
            });

            Hub.On<AnswerData>("AnswerRevealed", answer =>
            {
                OnAnswerRevealed?.Invoke(answer);
            });

            Hub.On<LeaderboardData[]>("ScoresUpdated", leaderboard =>
            {
                OnScoresUpdated?.Invoke(leaderboard);
            });

            Hub.On<LeaderboardData[]>("GameEnded", leaderboard =>
            {
                OnGameEnded?.Invoke(leaderboard);
            });

            Hub.On<string, int, string>("GameJoined", (sessionCode, totalPlayers, hostName) =>
            {
                OnGameJoined?.Invoke(sessionCode, totalPlayers, hostName);
            });

            Hub.On<string>("GameError", message =>
            {
                OnGameError?.Invoke(message);
            });
        }
    }
}
