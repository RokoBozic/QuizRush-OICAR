using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QuizRush.Core.Entities;
using QuizRush.Core.Hubs;
using QuizRush.Core.Services;
using QuizRush.Core.ViewModels;
using QuizRush.Infrastructure;
using QuizRush.Infrastructure.Services;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace QuizRush.Api.Hubs
{
    public class GameHub : Hub<IGameHubServer>
    {
        private readonly IGameSessionService _sessionService;
        private readonly IQuizService _quizService;
        private readonly QuizRushDbContext _dbContext;
        private readonly ScoreCalculationService _scoreService;

        private static readonly ConcurrentDictionary<string, GameSessionState> ActiveSessions = new();
        private static readonly ConcurrentDictionary<string, PlayerConnection> ConnectionIndex = new();

        public GameHub(
            IGameSessionService sessionService,
            IQuizService quizService,
            QuizRushDbContext dbContext,
            ScoreCalculationService scoreService)
        {
            _sessionService = sessionService;
            _quizService = quizService;
            _dbContext = dbContext;
            _scoreService = scoreService;
        }

        /// <summary>
        /// Creates a host-controlled game session and adds host connection to the SignalR group.
        /// </summary>
        public async Task HostGame(long quizId)
        {
            long? userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                await Clients.Caller.GameError("Only registered users can host games.");
                return;
            }

            try
            {
                var session = await _sessionService.CreateSessionAsync(quizId, userId.Value);
                await Groups.AddToGroupAsync(Context.ConnectionId, session.Code);

                ActiveSessions[session.Code] = new GameSessionState
                {
                    SessionId = session.Id,
                    SessionCode = session.Code,
                    HostUserId = userId.Value,
                    QuizId = quizId,
                    HostConnectionId = Context.ConnectionId
                };

                ConnectionIndex[Context.ConnectionId] = new PlayerConnection
                {
                    SessionCode = session.Code,
                    IsHost = true
                };

                await Clients.Caller.GameJoined(session.Code, 0, Context.User?.Identity?.Name ?? "Host");
            }
            catch (Exception ex)
            {
                await Clients.Caller.GameError(ex.Message);
            }
        }

        /// <summary>
        /// Starts the active session and broadcasts first question to the group.
        /// </summary>
        public async Task StartGame(string sessionCode)
        {
            if (!TryGetSession(sessionCode, out var session))
            {
                await Clients.Caller.GameError("Session not found.");
                return;
            }

            if (!IsHostCaller(session))
            {
                await Clients.Caller.GameError("Only host can start the game.");
                return;
            }

            var quiz = await _quizService.GetByIdAsync(session.QuizId);
            if (quiz == null || quiz.Questions.Count == 0)
            {
                await Clients.Group(sessionCode).SessionExpired();
                return;
            }

            session.CurrentQuestionIndex = 0;
            await SendQuestionToGroup(sessionCode, session, quiz.Questions.OrderBy(q => q.Id).ToList());
            await Clients.Group(sessionCode).GameStarted(quiz.Questions.Count);
        }

        /// <summary>
        /// Advances to the next question, or ends game if there are no more questions.
        /// </summary>
        public async Task NextQuestion(string sessionCode)
        {
            if (!TryGetSession(sessionCode, out var session))
            {
                await Clients.Caller.GameError("Session not found.");
                return;
            }

            if (!IsHostCaller(session))
            {
                await Clients.Caller.GameError("Only host can move to next question.");
                return;
            }

            var quiz = await _quizService.GetByIdAsync(session.QuizId);
            if (quiz == null || quiz.Questions.Count == 0)
            {
                await Clients.Group(sessionCode).SessionExpired();
                return;
            }

            var orderedQuestions = quiz.Questions.OrderBy(q => q.Id).ToList();

            if (!session.QuestionRevealed && session.CurrentQuestionId > 0)
            {
                var currentQuestion = await _dbContext.Questions
                    .Include(q => q.Answers)
                    .FirstOrDefaultAsync(q => q.Id == session.CurrentQuestionId);
                if (currentQuestion != null)
                {
                    var correct = currentQuestion.Answers.FirstOrDefault(a => a.IsCorrect);
                    if (correct != null)
                    {
                        session.QuestionRevealed = true;
                        await EndSubmissionPhase(sessionCode, session, currentQuestion, correct);
                    }
                }
            }

            if (session.CurrentQuestionIndex >= orderedQuestions.Count - 1)
            {
                await EndGame(sessionCode);
                return;
            }

            session.CurrentQuestionIndex++;
            await SendQuestionToGroup(sessionCode, session, orderedQuestions);
        }

        /// <summary>
        /// Ends the current game session and broadcasts the final leaderboard.
        /// </summary>
        public async Task EndGame(string sessionCode)
        {
            if (!TryGetSession(sessionCode, out var session))
            {
                await Clients.Caller.GameError("Session not found.");
                return;
            }

            if (!IsHostCaller(session))
            {
                await Clients.Caller.GameError("Only host can end the game.");
                return;
            }

            var leaderboard = await GetLeaderboard(sessionCode);

            var dbSession = await _dbContext.GameSessions.FindAsync(session.SessionId);
            if (dbSession != null)
            {
                dbSession.Status = GameStatus.Completed;
                dbSession.EndTime = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }

            await Clients.Group(sessionCode).GameEnded(leaderboard.ToArray());
            ActiveSessions.TryRemove(sessionCode, out _);
        }

        /// <summary>
        /// Adds a player to an active game session by PIN code.
        /// </summary>
        public async Task JoinGame(string sessionCode, string playerName)
        {
            if (!TryGetSession(sessionCode, out var session))
            {
                await Clients.Caller.GameError("Session not found.");
                return;
            }

            var player = new Player
            {
                GameSessionId = session.SessionId,
                Name = string.IsNullOrWhiteSpace(playerName) ? "Guest" : playerName.Trim(),
                UserId = GetCurrentUserId()
            };

            _dbContext.Players.Add(player);
            await _dbContext.SaveChangesAsync();

            await Groups.AddToGroupAsync(Context.ConnectionId, sessionCode);

            session.Players[Context.ConnectionId] = player;
            ConnectionIndex[Context.ConnectionId] = new PlayerConnection
            {
                SessionCode = sessionCode,
                IsHost = false
            };

            int totalPlayers = session.Players.Count;
            await Clients.Group(sessionCode).PlayerJoined(player.Name, totalPlayers);
            await Clients.Caller.GameJoined(sessionCode, totalPlayers, "Host");
        }

        /// <summary>
        /// Submits current player's answer and updates score and leaderboard state.
        /// </summary>
        public async Task SubmitAnswer(string sessionCode, long answerId)
        {
            if (!TryGetSession(sessionCode, out var session))
            {
                await Clients.Caller.GameError("Session not found.");
                return;
            }

            if (!session.Players.TryGetValue(Context.ConnectionId, out var player))
            {
                await Clients.Caller.GameError("Player not in session.");
                return;
            }

            bool alreadyAnswered = await _dbContext.PlayerAnswers
                .AnyAsync(pa => pa.PlayerId == player.Id && pa.QuestionId == session.CurrentQuestionId);
            if (alreadyAnswered)
            {
                await Clients.Caller.GameError("Already answered this question.");
                return;
            }

            var question = await _dbContext.Questions
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(q => q.Id == session.CurrentQuestionId);

            if (question == null)
            {
                await Clients.Caller.GameError("Question not found.");
                return;
            }

            var answer = question.Answers.FirstOrDefault(a => a.Id == answerId);
            if (answer == null)
            {
                await Clients.Caller.GameError("Answer does not belong to current question.");
                return;
            }

            int elapsedSeconds = (int)Math.Max(0, (DateTime.UtcNow - session.QuestionStartedAt).TotalSeconds);
            bool isCorrect = answer.IsCorrect;
            int basePoints = _scoreService.CalculatePoints(question.PointsValue, elapsedSeconds, isCorrect);
            int gamblePercent = session.GamblingPercentages.TryGetValue(player.Id, out int gp) ? gp : 0;
            int finalPoints = _scoreService.ApplyGambling(basePoints, gamblePercent, isCorrect);

            var playerAnswer = new PlayerAnswer
            {
                GameSessionId = session.SessionId,
                PlayerId = player.Id,
                QuestionId = question.Id,
                AnswerId = answer.Id,
                ScoreEarned = finalPoints,
                ResponseTime = TimeSpan.FromSeconds(elapsedSeconds),
                AnsweredAt = DateTime.UtcNow
            };

            _dbContext.PlayerAnswers.Add(playerAnswer);
            player.Score += finalPoints;
            await _dbContext.SaveChangesAsync();

            int playersAnswered = await _dbContext.PlayerAnswers
                .CountAsync(pa => pa.GameSessionId == session.SessionId && pa.QuestionId == question.Id);

            int totalPlayers = session.Players.Count;
            await Clients.Group(sessionCode).QuestionAnswered(playersAnswered, totalPlayers);

            if (playersAnswered >= totalPlayers && totalPlayers > 0 && !session.QuestionRevealed)
            {
                session.QuestionRevealed = true;
                await EndSubmissionPhase(sessionCode, session, question, answer);
            }
        }

        private async Task EndSubmissionPhase(string sessionCode, GameSessionState session, QuizRush.Core.Entities.Question question, QuizRush.Core.Entities.Answer submittedAnswer)
        {
            var correctAnswer = question.Answers.FirstOrDefault(a => a.IsCorrect) ?? submittedAnswer;

            await Clients.Group(sessionCode).SubmissionPhaseEnded();
            await Clients.Group(sessionCode).AllPlayersAnswered();
            await Clients.Group(sessionCode).AnswerRevealed(new AnswerData
            {
                AnswerId = correctAnswer.Id,
                Text = correctAnswer.Text,
                IsCorrect = correctAnswer.IsCorrect
            });
            await Clients.Group(sessionCode).ScoresUpdated((await GetLeaderboard(sessionCode)).ToArray());
        }

        /// <summary>
        /// Stores current player's gamble choice for the active question.
        /// </summary>
        public async Task PlaceGamble(string sessionCode, int gamblingPercentage)
        {
            if (gamblingPercentage < 0 || gamblingPercentage > 100)
            {
                await Clients.Caller.GameError("Gambling percentage must be between 0 and 100.");
                return;
            }

            if (!TryGetSession(sessionCode, out var session))
            {
                await Clients.Caller.GameError("Session not found.");
                return;
            }

            if (!session.Players.TryGetValue(Context.ConnectionId, out var player))
            {
                await Clients.Caller.GameError("Player not in session.");
                return;
            }

            session.GamblingPercentages[player.Id] = gamblingPercentage;

            var action = new GamblingAction
            {
                PlayerId = player.Id,
                GameSessionId = session.SessionId,
                QuestionId = session.CurrentQuestionId,
                PointsGambled = (int)(player.Score * (gamblingPercentage / 100.0)),
                Won = false
            };

            _dbContext.GamblingActions.Add(action);
            await _dbContext.SaveChangesAsync();

            await Clients.Caller.GamblingEnabled();
        }

        /// <summary>
        /// Removes a player from the current session group.
        /// </summary>
        public async Task LeaveGame(string sessionCode)
        {
            if (!TryGetSession(sessionCode, out var session))
            {
                return;
            }

            if (session.Players.TryRemove(Context.ConnectionId, out var player))
            {
                await Clients.Group(sessionCode).PlayerLeft(player.Name, session.Players.Count);
            }

            ConnectionIndex.TryRemove(Context.ConnectionId, out _);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionCode);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (ConnectionIndex.TryRemove(Context.ConnectionId, out var info))
            {
                await LeaveGame(info.SessionCode);
            }

            await base.OnDisconnectedAsync(exception);
        }

        private long? GetCurrentUserId()
        {
            string? id = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(id, out long parsedId) ? parsedId : null;
        }

        private bool TryGetSession(string sessionCode, out GameSessionState state)
        {
            return ActiveSessions.TryGetValue(sessionCode, out state!);
        }

        private bool IsHostCaller(GameSessionState session)
        {
            if (Context.ConnectionId == session.HostConnectionId)
            {
                return true;
            }

            long? userId = GetCurrentUserId();
            return userId.HasValue && userId.Value == session.HostUserId;
        }

        private async Task SendQuestionToGroup(string sessionCode, GameSessionState session, List<QuizRush.Core.Entities.Question> questions)
        {
            var question = questions[session.CurrentQuestionIndex];
            session.CurrentQuestionId = question.Id;
            session.QuestionStartedAt = DateTime.UtcNow;
            session.QuestionRevealed = false;
            session.GamblingPercentages.Clear();

            var payload = new QuestionData
            {
                QuestionId = question.Id,
                Text = question.Text,
                PointsValue = question.PointsValue,
                TimeLimit = question.TimeLimitSeconds,
                Answers = question.Answers.Select(a => new AnswerOptionData
                {
                    AnswerId = a.Id,
                    Text = a.Text
                }).ToList()
            };

            await Clients.Group(sessionCode).QuestionReady(payload, question.TimeLimitSeconds);
            await Clients.Group(sessionCode).QuestionDisplayed(session.CurrentQuestionIndex + 1, question.TimeLimitSeconds);
        }

        private async Task<List<LeaderboardData>> GetLeaderboard(string sessionCode)
        {
            var session = ActiveSessions[sessionCode];
            var players = await _dbContext.Players
                .Where(p => p.GameSessionId == session.SessionId)
                .OrderByDescending(p => p.Score)
                .ToListAsync();

            return players.Select((p, index) => new LeaderboardData
            {
                PlayerName = p.Name,
                Score = p.Score,
                Rank = index + 1
            }).ToList();
        }

        private sealed class GameSessionState
        {
            public long SessionId { get; set; }
            public string SessionCode { get; set; } = string.Empty;
            public long HostUserId { get; set; }
            public string HostConnectionId { get; set; } = string.Empty;
            public long QuizId { get; set; }
            public long CurrentQuestionId { get; set; }
            public int CurrentQuestionIndex { get; set; }
            public DateTime QuestionStartedAt { get; set; }
            public bool QuestionRevealed { get; set; }
            public ConcurrentDictionary<string, Player> Players { get; } = new();
            public ConcurrentDictionary<long, int> GamblingPercentages { get; } = new();
        }

        private sealed class PlayerConnection
        {
            public string SessionCode { get; set; } = string.Empty;
            public bool IsHost { get; set; }
        }
    }
}
