using Microsoft.EntityFrameworkCore;
using QuizRush.Core.Entities;
using QuizRush.Core.Services;
using QuizRush.Core.ViewModels;
using QuizRush.Infrastructure;

namespace QuizRush.Infrastructure.Services
{
    public class GameSessionService : IGameSessionService
    {
        private readonly QuizRushDbContext _context;
        private const string CodeChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const int CodeLength = 6;

        public GameSessionService(QuizRushDbContext context)
        {
            _context = context;
        }

        public async Task<GameSessionViewModel> CreateSessionAsync(long quizId, long hostUserId)
        {
            var quiz = await _context.Quizzes.FindAsync(quizId);
            if (quiz == null)
                throw new KeyNotFoundException($"Quiz with id {quizId} not found.");

            string code = await GenerateUniqueCodeAsync();

            var session = new GameSession
            {
                QuizId = quizId,
                HostUserId = hostUserId,
                Code = code,
                Status = GameStatus.WaitingToStart,
                StartTime = DateTime.UtcNow
            };

            _context.GameSessions.Add(session);
            await _context.SaveChangesAsync();

            return new GameSessionViewModel
            {
                Id = session.Id,
                Code = session.Code,
                QuizId = session.QuizId,
                QuizTitle = quiz.Title,
                Status = session.Status,
                StartTime = session.StartTime,
                PlayerCount = 0
            };
        }

        public async Task<GameSessionViewModel?> GetSessionByCodeAsync(string code)
        {
            var session = await _context.GameSessions
                .Include(s => s.Quiz)
                .Include(s => s.Players)
                .FirstOrDefaultAsync(s => s.Code == code);

            if (session == null)
                return null;

            return new GameSessionViewModel
            {
                Id = session.Id,
                Code = session.Code,
                QuizId = session.QuizId,
                QuizTitle = session.Quiz.Title,
                Status = session.Status,
                StartTime = session.StartTime,
                PlayerCount = session.Players.Count
            };
        }

        public async Task<IEnumerable<PlayerResultViewModel>> GetResultsAsync(long sessionId)
        {
            var session = await _context.GameSessions.FindAsync(sessionId);
            if (session == null)
                throw new KeyNotFoundException($"Session with id {sessionId} not found.");

            var players = await _context.Players
                .Where(p => p.GameSessionId == sessionId)
                .OrderByDescending(p => p.Score)
                .ToListAsync();

            return players.Select((p, index) => new PlayerResultViewModel
            {
                PlayerName = p.Name,
                Score = p.Score,
                Rank = index + 1
            });
        }

        private async Task<string> GenerateUniqueCodeAsync()
        {
            var random = new Random();
            string code;

            do
            {
                code = new string(Enumerable.Range(0, CodeLength)
                    .Select(_ => CodeChars[random.Next(CodeChars.Length)])
                    .ToArray());
            }
            while (await _context.GameSessions.AnyAsync(s => s.Code == code));

            return code;
        }
    }
}
