using Microsoft.EntityFrameworkCore;
using QuizRush.Core.ViewModels;
using QuizRush.Infrastructure;
using QuizRush.Infrastructure.Security;
using QuizRush.Infrastructure.Services;
using Xunit;

namespace QuizRush.Tests.User
{
    public class ChangePasswordWrongPasswordTest
    {
        [Fact]
        public async Task ChangePassword_WrongCurrentPassword_ThrowsUnauthorizedAccessException()
        {
            var options = new DbContextOptionsBuilder<QuizRushDbContext>()
                .UseInMemoryDatabase(databaseName: "ChangePasswordWrongPasswordDb")
                .Options;
            using var context = new QuizRushDbContext(options);
            var service = new UserService(context);

            var salt = PasswordHashProvider.GenerateSalt();
            var user = new QuizRush.Core.Entities.User
            {
                Username = "testuser",
                Email = "test@email.com",
                PasswordHash = PasswordHashProvider.HashPassword("correctpassword123", salt),
                PasswordSalt = salt
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var model = new ChangePasswordViewModel
            {
                CurrentPassword = "wrongpassword123",
                NewPassword = "newpassword123"
            };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.ChangePasswordAsync(user.Id, model));
        }
    }
}
