using Microsoft.EntityFrameworkCore;
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
