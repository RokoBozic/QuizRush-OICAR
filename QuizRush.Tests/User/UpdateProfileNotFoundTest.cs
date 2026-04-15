using Microsoft.EntityFrameworkCore;
using QuizRush.Core.ViewModels;
using QuizRush.Infrastructure;
using QuizRush.Infrastructure.Services;
using Xunit;

namespace QuizRush.Tests.User
{
    public class UpdateProfileNotFoundTest
    {
        [Fact]
        public async Task UpdateProfile_NonExistentUser_ThrowsKeyNotFoundException()
        {
            var options = new DbContextOptionsBuilder<QuizRushDbContext>()
                .UseInMemoryDatabase(databaseName: "UpdateProfileNotFoundDb")
                .Options;
            using var context = new QuizRushDbContext(options);
            var service = new UserService(context);

            var model = new UpdateProfileViewModel
            {
                Username = "someusername",
                Email = "some@email.com"
            };

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.UpdateProfileAsync(999, model));
        }
    }
}
