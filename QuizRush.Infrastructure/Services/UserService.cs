using Microsoft.EntityFrameworkCore;
using QuizRush.Core.Entities;
using QuizRush.Core.Services;
using QuizRush.Core.ViewModels;
using QuizRush.Infrastructure;
using QuizRush.Infrastructure.Security;

namespace QuizRush.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly QuizRushDbContext _context;

        public UserService(QuizRushDbContext context)
        {
            _context = context;
        }

        public async Task<UserProfileViewModel?> GetProfileAsync(long userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return null;

            return new UserProfileViewModel
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                AccumulatedPoints = user.AccumulatedPoints,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<PlayerStatsViewModel> GetPlayerStatsAsync(long userId)
        {
            var completedRows = await (
                from p in _context.Players.AsNoTracking()
                join gs in _context.GameSessions.AsNoTracking() on p.GameSessionId equals gs.Id
                where p.UserId == userId && gs.Status == GameStatus.Completed
                select new
                {
                    p.Id,
                    p.Score,
                    p.GameSessionId,
                    gs.Code,
                    EndedAt = gs.EndTime ?? gs.StartTime
                }).ToListAsync();

            if (completedRows.Count == 0)
            {
                return new PlayerStatsViewModel();
            }

            var uniqueBySession = completedRows
                .GroupBy(r => r.GameSessionId)
                .Select(g => g.OrderByDescending(x => x.Id).First())
                .ToList();

            var sessionIds = uniqueBySession.Select(r => r.GameSessionId).Distinct().ToList();
            var allSessionPlayers = await _context.Players.AsNoTracking()
                .Where(p => sessionIds.Contains(p.GameSessionId))
                .Select(p => new { p.GameSessionId, p.Score })
                .ToListAsync();

            var history = new List<PlayerGameHistoryItemViewModel>();
            foreach (var row in uniqueBySession)
            {
                int rank = allSessionPlayers.Count(x => x.GameSessionId == row.GameSessionId && x.Score > row.Score) + 1;
                history.Add(new PlayerGameHistoryItemViewModel
                {
                    SessionCode = row.Code,
                    Date = row.EndedAt,
                    Score = row.Score,
                    Rank = rank
                });
            }

            history = history.OrderByDescending(h => h.Date).ToList();

            return new PlayerStatsViewModel
            {
                GamesPlayed = history.Count,
                GamesWon = history.Count(h => h.Rank == 1),
                HighestScore = history.Count == 0 ? 0 : history.Max(h => h.Score),
                AccumulatedPoints = history.Sum(h => h.Score),
                GameHistory = history
            };
        }

        public async Task UpdateProfileAsync(long userId, UpdateProfileViewModel model)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new KeyNotFoundException($"User with id {userId} not found.");

            if (await _context.Users.AnyAsync(u => u.Username == model.Username && u.Id != userId))
                throw new InvalidOperationException("Username is already taken.");

            if (await _context.Users.AnyAsync(u => u.Email == model.Email && u.Id != userId))
                throw new InvalidOperationException("Email is already in use.");

            user.Username = model.Username;
            user.Email = model.Email;

            await _context.SaveChangesAsync();
        }

        public async Task ChangePasswordAsync(long userId, ChangePasswordViewModel model)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new KeyNotFoundException($"User with id {userId} not found.");

            if (!PasswordHashProvider.VerifyPassword(model.CurrentPassword, user.PasswordHash, user.PasswordSalt))
                throw new UnauthorizedAccessException("Current password is incorrect.");

            string newSalt = PasswordHashProvider.GenerateSalt();
            string newHash = PasswordHashProvider.HashPassword(model.NewPassword, newSalt);

            user.PasswordHash = newHash;
            user.PasswordSalt = newSalt;

            await _context.SaveChangesAsync();
        }
    }
}
