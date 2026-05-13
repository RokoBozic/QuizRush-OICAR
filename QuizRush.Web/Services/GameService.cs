using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using QuizRush.Core.ViewModels;

namespace QuizRush.Web.Services
{
    public class GameService : IAsyncDisposable
    {
        private readonly AuthService _authService;
        private readonly string _hubUrl;
        private HubConnection? _hubConnection;

        public event Action<string, int>? OnPlayerJoined;
        public event Action<int>? OnGamblingPhaseStarted;
        public event Action<QuestionData, int>? OnQuestionReady;
        public event Action<AnswerData>? OnAnswerRevealed;
        public event Action<LeaderboardData[]>? OnScoresUpdated;
        public event Action<LeaderboardData[]>? OnGameEnded;
        public event Action<string>? OnGameError;

        public event Action<string, int, string>? OnGameJoined;

        public event Action<int>? OnGameStarted;

        public event Action<string>? OnHostSelfAck;

        public event Action<string>? OnHostPlayerNotice;

        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
        private HubConnection Hub => _hubConnection ?? throw new InvalidOperationException("Hub connection not initialized.");

        public GameService(AuthService authService, IConfiguration configuration)
        {
            _authService = authService;
            string baseUrl = configuration["QuizRush:ApiBaseUrl"] ?? "http://localhost:5176";
            _hubUrl = $"{baseUrl.TrimEnd('/')}/hub/game";
        }

        public async Task DisconnectAsync()
        {
            if (_hubConnection is null)
            {
                return;
            }

            try
            {
                await _hubConnection.StopAsync();
            }
            catch
            {
                // ignore stop errors during logout
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

        public async Task EnsureConnectedAsync()
        {
            if (_hubConnection == null)
            {
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(_hubUrl, options =>
                    {
                        options.AccessTokenProvider = async () => await _authService.GetStoredTokenAsync();
                    })
                    .WithAutomaticReconnect()
                    .Build();

                ConfigureServerEvents();
            }

            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                await _hubConnection.StartAsync().ConfigureAwait(false);
            }
        }

        public async Task HostGame(long quizId)
        {
            await EnsureConnectedAsync();
            await Hub.InvokeAsync("HostGame", quizId).ConfigureAwait(false);
        }

        public async Task RejoinGame(string sessionCode)
        {
            await EnsureConnectedAsync();
            await Hub.InvokeAsync("RejoinGame", sessionCode).ConfigureAwait(false);
        }

        public async Task JoinGame(string sessionCode, string playerName)
        {
            await EnsureConnectedAsync();
            await Hub.InvokeAsync("JoinGame", sessionCode, playerName).ConfigureAwait(false);
        }

        public async Task StartGame(string sessionCode)
        {
            await EnsureConnectedAsync();
            await Hub.InvokeAsync("StartGame", sessionCode).ConfigureAwait(false);
        }

        public async Task NextQuestion(string sessionCode)
        {
            await EnsureConnectedAsync();
            await Hub.InvokeAsync("NextQuestion", sessionCode).ConfigureAwait(false);
        }

        public async Task SubmitAnswer(string sessionCode, long answerId)
        {
            await EnsureConnectedAsync();
            await Hub.InvokeAsync("SubmitAnswer", sessionCode, answerId).ConfigureAwait(false);
        }

        public async Task PlaceGamble(string sessionCode, int percentage)
        {
            await EnsureConnectedAsync();
            await Hub.InvokeAsync("PlaceGamble", sessionCode, percentage).ConfigureAwait(false);
        }

        public async Task EndGame(string sessionCode)
        {
            await EnsureConnectedAsync();
            await Hub.InvokeAsync("EndGame", sessionCode).ConfigureAwait(false);
        }

        private void ConfigureServerEvents()
        {
            Hub.On<string, int>("PlayerJoined", (name, totalPlayers) =>
            {
                OnPlayerJoined?.Invoke(name, totalPlayers);
            });

            Hub.On<int>("GamblingPhaseStarted", seconds =>
            {
                OnGamblingPhaseStarted?.Invoke(seconds);
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

            Hub.On<int>("GameStarted", questionCount =>
            {
                OnGameStarted?.Invoke(questionCount);
            });

            Hub.On<string>("HostSelfAck", message =>
            {
                OnHostSelfAck?.Invoke(message);
            });

            Hub.On<string>("HostPlayerNotice", message =>
            {
                OnHostPlayerNotice?.Invoke(message);
            });
        }
    }
}
