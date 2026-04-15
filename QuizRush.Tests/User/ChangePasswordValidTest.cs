using Microsoft.EntityFrameworkCore;
using QuizRush.Core.ViewModels;
using QuizRush.Infrastructure;
using QuizRush.Infrastructure.Security;
using QuizRush.Infrastructure.Services;
using Xunit;

namespace QuizRush.Tests.User
{
    public class ChangePasswordValidTest
    {
        [Fact]
        public async Task ChangePassword_CorrectCurrentPassword_UpdatesPassword()
        {
            var options = new DbContextOptionsBuilder<QuizRushDbContext>()
                .UseInMemoryDatabase(databaseName: "ChangePasswordValidDb")
                .Options;
            using var context = new QuizRushDbContext(options);
            var service = new UserService(context);

            var salt = PasswordHashProvider.GenerateSalt();
            var user = new QuizRush.Core.Entities.User
            {
                Username = "testuser",
                Email = "test@email.com",
                PasswordHash = PasswordHashProvider.HashPassword("oldpassword123", salt),
                PasswordSalt = salt
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var model = new ChangePasswordViewModel
            {
                CurrentPassword = "oldpassword123",
                NewPassword = "newpassword123"
            };

            await service.ChangePasswordAsync(user.Id, model);

            var updated = await context.Users.FindAsync(user.Id);
            Assert.True(PasswordHashProvider.VerifyPassword("newpassword123", updated!.PasswordHash, updated.PasswordSalt));
        }
    }
}
