using Microsoft.EntityFrameworkCore;
using QuizRush.Core.Entities;
using QuizRush.Core.ViewModels;
using QuizRush.Infrastructure;
using QuizRush.Infrastructure.Security;
using QuizRush.Infrastructure.Services;
using Xunit;

namespace QuizRush.Tests.User
{
    public class UpdateProfileValidTest
    {
        [Fact]
        public async Task UpdateProfile_ValidData_UpdatesUsernameAndEmail()
        {
            var options = new DbContextOptionsBuilder<QuizRushDbContext>()
                .UseInMemoryDatabase(databaseName: "UpdateProfileValidDb")
                .Options;
            using var context = new QuizRushDbContext(options);
            var service = new UserService(context);

            var salt = PasswordHashProvider.GenerateSalt();
            var user = new QuizRush.Core.Entities.User
            {
                Username = "oldusername",
                Email = "old@email.com",
                PasswordHash = PasswordHashProvider.HashPassword("password123", salt),
                PasswordSalt = salt
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var model = new UpdateProfileViewModel
            {
                Username = "newusername",
                Email = "new@email.com"
            };

            await service.UpdateProfileAsync(user.Id, model);

            var updated = await context.Users.FindAsync(user.Id);
            Assert.Equal("newusername", updated!.Username);
            Assert.Equal("new@email.com", updated.Email);
        }
    }
}
