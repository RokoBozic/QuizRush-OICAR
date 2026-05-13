using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuizRush.Core.Entities;
using QuizRush.Core.Hubs;
using QuizRush.Core.Services;
using QuizRush.Core.ViewModels;
using QuizRush.Infrastructure;
using QuizRush.Infrastructure.Services;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Threading;

namespace QuizRush.Api.Hubs
{
    public class GameHub : Hub<IGameHubServer>
    {
        private const int QuestionTimeBufferSeconds = 2;
        private const int GamblingPhaseSeconds = 15;
        private const int AutoAdvanceAfterSubmissionSeconds = 3;
        private const int InitialPlayerScore = 100;

        private static string NormalizeSessionCode(string? sessionCode) =>
            string.IsNullOrWhiteSpace(sessionCode) ? string.Empty : sessionCode.Trim().ToUpperInvariant();

        private readonly IGameSessionService _sessionService;
        private readonly IQuizService _quizService;
        private readonly QuizRushDbContext _dbContext;
        private readonly ScoreCalculationService _scoreService;
        private readonly IServiceScopeFactory _scopeFactory;

        private static readonly ConcurrentDictionary<string, GameSessionState> ActiveSessions = new();
        private static readonly ConcurrentDictionary<string, PlayerConnection> ConnectionIndex = new();

        public GameHub(
            IGameSessionService sessionService,
            IQuizService quizService,
            QuizRushDbContext dbContext,
            ScoreCalculationService scoreService,
            IServiceScopeFactory scopeFactory)
        {
            _sessionService = sessionService;
            _quizService = quizService;
            _dbContext = dbContext;
            _scoreService = scoreService;
            _scopeFactory = scopeFactory;
        }

        /// <summary>
        /// Rejoin an existing game session as host (for page refreshes).
        /// </summary>
        public async Task RejoinGame(string sessionCode)
        {
            if (!TryGetSession(sessionCode, out var session))
            {
                await Clients.Caller.GameError("Session not found.");
                return;
            }

            long? userId = GetCurrentUserId();
            if (!userId.HasValue || session.HostUserId != userId.Value)
            {
                await Clients.Caller.GameError("Only the original host can rejoin this session.");
                return;
            }

            string code = session.SessionCode;
            await Groups.AddToGroupAsync(Context.ConnectionId, code);
            session.HostConnectionId = Context.ConnectionId;

            ConnectionIndex[Context.ConnectionId] = new PlayerConnection
            {
                SessionCode = code,
                IsHost = true
            };

            int playerCount = session.Players.Count;
            await Clients.Caller.GameJoined(code, playerCount, Context.User?.Identity?.Name ?? "Host");

            if (session.GameLive)
            {
                var rejoinQuiz = await _quizService.GetByIdForCreatorAsync(session.QuizId, session.HostUserId);
                int questionCount = rejoinQuiz?.Questions.Count ?? 0;
                await Clients.Caller.GameStarted(questionCount);
            }

            if (session.GameLive && session.CurrentQuestionId > 0)
            {
                var question = await _dbContext.Questions
                    .Include(q => q.Answers)
                    .FirstOrDefaultAsync(q => q.Id == session.CurrentQuestionId);
                if (question != null)
                {
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
                    await Clients.Caller.QuestionReady(payload, question.TimeLimitSeconds);
                }
            }

            var leaderboard = await GetLeaderboard(session, _dbContext);
            await Clients.Caller.ScoresUpdated(leaderboard.ToArray());
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

            if (ActiveSessions.Values.Any(s => s.HostUserId == userId.Value))
            {
                await Clients.Caller.GameError("You already have an active game session. End that game before hosting another quiz.");
                return;
            }

            try
            {
                var session = await _sessionService.CreateSessionAsync(quizId, userId.Value);
                string code = NormalizeSessionCode(session.Code);
                await Groups.AddToGroupAsync(Context.ConnectionId, code);

                ActiveSessions[code] = new GameSessionState
                {
                    SessionId = session.Id,
                    SessionCode = code,
                    HostUserId = userId.Value,
                    QuizId = quizId,
                    HostConnectionId = Context.ConnectionId
                };

                ConnectionIndex[Context.ConnectionId] = new PlayerConnection
                {
                    SessionCode = code,
                    IsHost = true
                };

                await Clients.Caller.GameJoined(code, 0, Context.User?.Identity?.Name ?? "Host");
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

            string code = session.SessionCode;

            if (!IsHostCaller(session))
            {
                await Clients.Caller.GameError("Only host can start the game.");
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, code);
            session.HostConnectionId = Context.ConnectionId;

            if (session.GameLive)
            {
                await Clients.Caller.GameError("This game has already been started.");
                return;
            }

            var quiz = await _quizService.GetByIdForCreatorAsync(session.QuizId, session.HostUserId);
            if (quiz == null || quiz.Questions.Count == 0)
            {
                await Clients.Group(code).SessionExpired();
                return;
            }

            session.GameLive = true;
            session.CurrentQuestionIndex = 0;
            await Clients.Group(code).GameStarted(quiz.Questions.Count);
            try
            {
                await SendQuestionToGroup(code, session, quiz.Questions.OrderBy(q => q.Id).ToList());
            }
            catch (Exception ex)
            {
                session.GameLive = false;
                await Clients.Caller.GameError($"Could not start the round: {ex.Message}");
            }
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

            string code = session.SessionCode;

            CancelSubmissionAutoAdvance(session);
            CancelGamblingPhase(session);

            if (!IsHostCaller(session))
            {
                await Clients.Caller.GameError("Only host can move to next question.");
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, code);
            session.HostConnectionId = Context.ConnectionId;

            if (!session.GameLive)
            {
                await Clients.Caller.GameError("Start the game before moving to the next question.");
                return;
            }

            var quiz = await _quizService.GetByIdForCreatorAsync(session.QuizId, session.HostUserId);
            if (quiz == null || quiz.Questions.Count == 0)
            {
                await Clients.Group(code).SessionExpired();
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
                    await FinalizeQuestionPhaseAsync(code, session, currentQuestion, _dbContext, scheduleAutoAdvanceAfterSubmission: false);
                }
            }

            if (session.CurrentQuestionIndex >= orderedQuestions.Count - 1)
            {
                await EndGameAsync(code, endedAfterLastQuestionAdvance: true);
                return;
            }

            await Clients.Caller.HostSelfAck("You moved to the next question.");
            await Clients.OthersInGroup(code).HostPlayerNotice("Next question started by host.");

            session.CurrentQuestionIndex++;
            await SendQuestionToGroup(code, session, orderedQuestions);
        }

        /// <summary>
        /// Ends the current game session and broadcasts the final leaderboard.
        /// </summary>
        public Task EndGame(string sessionCode) => EndGameAsync(sessionCode, endedAfterLastQuestionAdvance: false);

        private async Task EndGameAsync(string sessionCode, bool endedAfterLastQuestionAdvance)
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

            string code = session.SessionCode;

            await Groups.AddToGroupAsync(Context.ConnectionId, code);
            session.HostConnectionId = Context.ConnectionId;

            string hostAck = endedAfterLastQuestionAdvance
                ? "There are no more questions. The game has ended."
                : "You ended the game.";
            await Clients.Caller.HostSelfAck(hostAck);
            await Clients.OthersInGroup(code).HostPlayerNotice("Game ended by host.");

            await CompleteGameSessionCoreAsync(code, session, _dbContext);
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

            string code = session.SessionCode;

            string normalizedName = string.IsNullOrWhiteSpace(playerName) ? "Guest" : playerName.Trim();
            long? userId = GetCurrentUserId();

            var existingPlayer = await _dbContext.Players
                .FirstOrDefaultAsync(p => p.GameSessionId == session.SessionId && p.Name == normalizedName);

            Player player;
            if (existingPlayer != null)
            {
                player = existingPlayer;
                foreach (var kvp in session.Players.ToArray())
                {
                    if (kvp.Value.Id == player.Id)
                    {
                        if (session.Players.TryRemove(kvp.Key, out _))
                        {
                            ConnectionIndex.TryRemove(kvp.Key, out _);
                            await Groups.RemoveFromGroupAsync(kvp.Key, code);
                        }
                    }
                }
            }
            else
            {
                player = new Player
                {
                    GameSessionId = session.SessionId,
                    Name = normalizedName,
                    UserId = userId,
                    Score = InitialPlayerScore
                };

                _dbContext.Players.Add(player);
                await _dbContext.SaveChangesAsync();
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, code);

            session.Players[Context.ConnectionId] = player;
            ConnectionIndex[Context.ConnectionId] = new PlayerConnection
            {
                SessionCode = code,
                IsHost = false
            };

            int totalPlayers = session.Players.Count;
            if (existingPlayer == null)
            {
                await Clients.Group(code).PlayerJoined(player.Name, totalPlayers);
            }

            await Clients.Caller.GameJoined(code, totalPlayers, "Host");
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

            string code = session.SessionCode;

            if (!session.Players.TryGetValue(Context.ConnectionId, out var player))
            {
                await Clients.Caller.GameError("Player not in session.");
                return;
            }

            if (session.GameEnded)
            {
                await Clients.Caller.GameError("Game has ended.");
                return;
            }

            if (session.InGamblingPhase)
            {
                await Clients.Caller.GameError("Wait until the question appears before answering.");
                return;
            }

            if (session.QuestionRevealed)
            {
                await Clients.Caller.GameError("Question phase has ended.");
                return;
            }

            if (!session.SubmittedConnectionIdsForQuestion.TryAdd(Context.ConnectionId, 0))
            {
                await Clients.Caller.GameError("Already answered this question.");
                return;
            }

            bool answerCommitted = false;
            try
            {
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
                int scoreBeforeAnswer = player.Score;
                int questionPoints = question.PointsValue > 0 ? question.PointsValue : 100;
                int basePoints = _scoreService.CalculatePoints(questionPoints, elapsedSeconds, isCorrect);
                int gamblePercent = session.GamblingPercentages.TryGetValue(Context.ConnectionId, out int gp) ? gp : 0;
                int finalPoints = _scoreService.ApplyGambling(basePoints, scoreBeforeAnswer, gamblePercent, isCorrect);

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
                _dbContext.Players.Update(player);
                await _dbContext.SaveChangesAsync();
                answerCommitted = true;

                // Send immediate personal feedback to the player about THEIR answer
                await Clients.Caller.AnswerRevealed(new AnswerData
                {
                    AnswerId = answer.Id,
                    Text = answer.Text,
                    IsCorrect = isCorrect,
                    BasePoints = basePoints,
                    GamblingBonus = finalPoints - basePoints,
                    TotalPoints = finalPoints
                });

                int playersAnswered = await _dbContext.PlayerAnswers
                    .CountAsync(pa => pa.GameSessionId == session.SessionId && pa.QuestionId == question.Id);

                int totalPlayers = session.Players.Count;
                await Clients.Group(code).QuestionAnswered(playersAnswered, totalPlayers);

                if (playersAnswered >= totalPlayers && totalPlayers > 0)
                {
                    await FinalizeQuestionPhaseAsync(code, session, question, _dbContext);
                }
            }
            finally
            {
                if (!answerCommitted)
                {
                    session.SubmittedConnectionIdsForQuestion.TryRemove(Context.ConnectionId, out _);
                }
            }
        }

        private void CancelQuestionPhaseTimer(GameSessionState session)
        {
            session.QuestionPhaseCts?.Cancel();
            session.QuestionPhaseCts?.Dispose();
            session.QuestionPhaseCts = null;
        }

        private void CancelGamblingPhase(GameSessionState session)
        {
            session.GamblingPhaseCts?.Cancel();
            session.GamblingPhaseCts?.Dispose();
            session.GamblingPhaseCts = null;
            session.InGamblingPhase = false;
        }

        private void CancelSubmissionAutoAdvance(GameSessionState session)
        {
            session.SubmissionAutoAdvanceCts?.Cancel();
            session.SubmissionAutoAdvanceCts?.Dispose();
            session.SubmissionAutoAdvanceCts = null;
        }

        private async Task CompleteGameSessionCoreAsync(string code, GameSessionState session, QuizRushDbContext db)
        {
            CancelQuestionPhaseTimer(session);
            CancelGamblingPhase(session);
            CancelSubmissionAutoAdvance(session);
            session.GameEnded = true;

            var leaderboard = await GetLeaderboard(session, db);

            var dbSession = await db.GameSessions.FindAsync(session.SessionId);
            if (dbSession != null)
            {
                dbSession.Status = GameStatus.Completed;
                dbSession.EndTime = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }

            await Clients.Group(code).GameEnded(leaderboard.ToArray());

            ConnectionIndex.TryRemove(session.HostConnectionId, out _);
            foreach (var connectionId in session.Players.Keys.ToArray())
            {
                ConnectionIndex.TryRemove(connectionId, out _);
            }

            ActiveSessions.TryRemove(code, out _);
        }

        private async Task RunAutoAdvanceToNextQuestionAsync(string sessionCode, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(AutoAdvanceAfterSubmissionSeconds), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (!TryGetSession(sessionCode, out var session))
            {
                return;
            }

            if (!session.GameLive || session.GameEnded)
            {
                return;
            }

            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<QuizRushDbContext>();
            var quizService = scope.ServiceProvider.GetRequiredService<IQuizService>();

            var quiz = await quizService.GetByIdForCreatorAsync(session.QuizId, session.HostUserId);
            if (quiz == null || quiz.Questions.Count == 0)
            {
                await Clients.Group(sessionCode).SessionExpired();
                return;
            }

            var orderedQuestions = quiz.Questions.OrderBy(q => q.Id).ToList();

            string code = session.SessionCode;

            if (session.CurrentQuestionIndex >= orderedQuestions.Count - 1)
            {
                if (!string.IsNullOrEmpty(session.HostConnectionId))
                {
                    await Clients.Client(session.HostConnectionId).HostSelfAck("There are no more questions. The game has ended.");
                    await Clients.GroupExcept(code, session.HostConnectionId).HostPlayerNotice("Game ended by host.");
                }
                else
                {
                    await Clients.Group(code).HostPlayerNotice("Game ended.");
                }

                await CompleteGameSessionCoreAsync(code, session, db);
                return;
            }

            if (!string.IsNullOrEmpty(session.HostConnectionId))
            {
                await Clients.Client(session.HostConnectionId).HostSelfAck("Next question starting automatically.");
                await Clients.GroupExcept(code, session.HostConnectionId).HostPlayerNotice("Next question.");
            }
            else
            {
                await Clients.Group(code).HostPlayerNotice("Next question.");
            }

            session.CurrentQuestionIndex++;
            await SendQuestionToGroup(code, session, orderedQuestions);
        }

        private void StartQuestionPhaseTimer(string sessionCode, GameSessionState session, QuizRush.Core.Entities.Question question, int timeLimitSeconds)
        {
            CancelQuestionPhaseTimer(session);
            var cts = new CancellationTokenSource();
            session.QuestionPhaseCts = cts;
            long questionId = question.Id;
            _ = RunQuestionPhaseTimeoutAsync(sessionCode, questionId, timeLimitSeconds, cts.Token);
        }

        private async Task RunQuestionPhaseTimeoutAsync(string sessionCode, long questionId, int timeLimitSeconds, CancellationToken cancellationToken)
        {
            try
            {
                int delayMs = (timeLimitSeconds + QuestionTimeBufferSeconds) * 1000;
                await Task.Delay(delayMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (!TryGetSession(sessionCode, out var session))
            {
                return;
            }

            if (session.QuestionRevealed || session.CurrentQuestionId != questionId)
            {
                return;
            }

            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<QuizRushDbContext>();
            var question = await db.Questions
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(q => q.Id == questionId);

            if (question == null)
            {
                return;
            }

            await FinalizeQuestionPhaseAsync(sessionCode, session, question, db);
        }

        private async Task FinalizeQuestionPhaseAsync(string sessionCode, GameSessionState session, QuizRush.Core.Entities.Question question, QuizRushDbContext db, bool scheduleAutoAdvanceAfterSubmission = true)
        {
            await session.RevealPhaseMutex.WaitAsync();
            try
            {
                if (!TryGetSession(sessionCode, out var live) || !ReferenceEquals(live, session))
                {
                    return;
                }

                if (session.QuestionRevealed || session.CurrentQuestionId != question.Id)
                {
                    return;
                }

                var revealAnswer = question.Answers.FirstOrDefault(a => a.IsCorrect)
                    ?? question.Answers.FirstOrDefault();

                if (revealAnswer == null)
                {
                    return;
                }

                session.QuestionRevealed = true;
                CancelQuestionPhaseTimer(session);

                await EndSubmissionPhase(sessionCode, session, question, revealAnswer, db, scheduleAutoAdvanceAfterSubmission);
            }
            finally
            {
                session.RevealPhaseMutex.Release();
            }
        }

        private async Task EndSubmissionPhase(string sessionCode, GameSessionState session, QuizRush.Core.Entities.Question question, QuizRush.Core.Entities.Answer submittedAnswer, QuizRushDbContext db, bool scheduleAutoAdvanceAfterSubmission)
        {
            var correctAnswer = question.Answers.FirstOrDefault(a => a.IsCorrect) ?? submittedAnswer;

            await Clients.Group(sessionCode).SubmissionPhaseEnded();
            await Clients.Group(sessionCode).AllPlayersAnswered();

            // Send the correct answer ONLY to the host - players already got personal feedback in SubmitAnswer
            if (!string.IsNullOrEmpty(session.HostConnectionId))
            {
                await Clients.Client(session.HostConnectionId).AnswerRevealed(new AnswerData
                {
                    AnswerId = correctAnswer.Id,
                    Text = correctAnswer.Text,
                    IsCorrect = correctAnswer.IsCorrect
                });
            }

            await Clients.Group(sessionCode).ScoresUpdated((await GetLeaderboard(session, db)).ToArray());

            if (scheduleAutoAdvanceAfterSubmission)
            {
                CancelSubmissionAutoAdvance(session);
                var cts = new CancellationTokenSource();
                session.SubmissionAutoAdvanceCts = cts;
                _ = RunAutoAdvanceToNextQuestionAsync(sessionCode, cts.Token);
            }
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

            if (!session.InGamblingPhase)
            {
                await Clients.Caller.GameError("Gambling is only allowed during the gambling phase.");
                return;
            }

            session.GamblingPercentages[Context.ConnectionId] = gamblingPercentage;

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

            string code = session.SessionCode;

            if (session.Players.TryRemove(Context.ConnectionId, out var player))
            {
                await Clients.Group(code).PlayerLeft(player.Name, session.Players.Count);
            }

            ConnectionIndex.TryRemove(Context.ConnectionId, out _);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, code);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (ConnectionIndex.TryRemove(Context.ConnectionId, out var info))
            {
                string code = NormalizeSessionCode(info.SessionCode);
                if (TryGetSession(code, out var session))
                {
                    if (info.IsHost && session.HostConnectionId == Context.ConnectionId)
                    {
                        await AbandonSessionAsHostDisconnectedAsync(code, session);
                    }
                    else if (!info.IsHost)
                    {
                        await LeaveGame(code);
                    }
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Drops the in-memory session when the host disconnects so they can host again and players are notified.
        /// </summary>
        private async Task AbandonSessionAsHostDisconnectedAsync(string code, GameSessionState session)
        {
            CancelQuestionPhaseTimer(session);
            CancelGamblingPhase(session);
            CancelSubmissionAutoAdvance(session);
            foreach (var connectionId in session.Players.Keys.ToArray())
            {
                ConnectionIndex.TryRemove(connectionId, out _);
                await Groups.RemoveFromGroupAsync(connectionId, code);
            }

            ConnectionIndex.TryRemove(session.HostConnectionId, out _);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, code);
            await Clients.Group(code).SessionExpired();
            ActiveSessions.TryRemove(code, out _);
        }

        private long? GetCurrentUserId()
        {
            string? id = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(id, out long parsedId) ? parsedId : null;
        }

        private bool TryGetSession(string sessionCode, out GameSessionState state)
        {
            string key = NormalizeSessionCode(sessionCode);
            if (string.IsNullOrEmpty(key))
            {
                state = null!;
                return false;
            }

            return ActiveSessions.TryGetValue(key, out state!);
        }

        private bool IsHostCaller(GameSessionState session)
        {
            if (Context.ConnectionId == session.HostConnectionId)
            {
                return true;
            }

            long? userId = GetCurrentUserId();
            if (userId.HasValue && userId.Value == session.HostUserId)
            {
                // Same host after SignalR reconnect (new connection id) — keep hub state aligned
                session.HostConnectionId = Context.ConnectionId;
                return true;
            }

            return false;
        }

        private async Task SendQuestionToGroup(string sessionCode, GameSessionState session, List<QuizRush.Core.Entities.Question> questions)
        {
            int generation = Interlocked.Increment(ref session.QuestionLifecycleGeneration);
            CancelQuestionPhaseTimer(session);
            CancelGamblingPhase(session);
            CancelSubmissionAutoAdvance(session);

            var question = questions[session.CurrentQuestionIndex];
            session.CurrentQuestionId = question.Id;
            session.QuestionRevealed = false;
            session.GamblingPercentages.Clear();
            session.SubmittedConnectionIdsForQuestion.Clear();

            bool gamblingThisRound = session.CurrentQuestionIndex > 0;

            if (!gamblingThisRound)
            {
                session.GamblingPhaseCts = null;
                session.InGamblingPhase = false;
                await Clients.Group(sessionCode).GamblingPhaseStarted(0);
            }
            else
            {
                var gamblingCts = new CancellationTokenSource();
                session.GamblingPhaseCts = gamblingCts;
                session.InGamblingPhase = true;

                await Clients.Group(sessionCode).GamblingPhaseStarted(GamblingPhaseSeconds);

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(GamblingPhaseSeconds), gamblingCts.Token);
                }
                catch (OperationCanceledException)
                {
                    if (!TryGetSession(sessionCode, out var checkSession) || checkSession.QuestionLifecycleGeneration != generation)
                    {
                        return;
                    }
                }
                finally
                {
                    if (TryGetSession(sessionCode, out var phaseSession) && phaseSession.QuestionLifecycleGeneration == generation)
                    {
                        phaseSession.InGamblingPhase = false;
                    }
                }
            }

            if (!TryGetSession(sessionCode, out var liveSession) || liveSession.QuestionLifecycleGeneration != generation)
            {
                return;
            }

            liveSession.QuestionStartedAt = DateTime.UtcNow;

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
            await Clients.Group(sessionCode).QuestionDisplayed(liveSession.CurrentQuestionIndex + 1, question.TimeLimitSeconds);

            if (!TryGetSession(sessionCode, out liveSession) || liveSession.QuestionLifecycleGeneration != generation)
            {
                return;
            }

            StartQuestionPhaseTimer(sessionCode, liveSession, question, question.TimeLimitSeconds);
        }

        private async Task<List<LeaderboardData>> GetLeaderboard(GameSessionState session, QuizRushDbContext db)
        {
            var players = await db.Players
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
            public ConcurrentDictionary<string, int> GamblingPercentages { get; } = new();
            public ConcurrentDictionary<string, byte> SubmittedConnectionIdsForQuestion { get; } = new();
            public CancellationTokenSource? QuestionPhaseCts { get; set; }
            public CancellationTokenSource? GamblingPhaseCts { get; set; }
            public CancellationTokenSource? SubmissionAutoAdvanceCts { get; set; }
            public bool InGamblingPhase { get; set; }
            public int QuestionLifecycleGeneration;
            public SemaphoreSlim RevealPhaseMutex { get; } = new(1, 1);
            public bool GameLive { get; set; }
            public bool GameEnded { get; set; }
        }

        private sealed class PlayerConnection
        {
            public string SessionCode { get; set; } = string.Empty;
            public bool IsHost { get; set; }
        }
    }
}
