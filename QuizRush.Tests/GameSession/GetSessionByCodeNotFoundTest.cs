using Microsoft.EntityFrameworkCore;
using QuizRush.Infrastructure;
using QuizRush.Infrastructure.Services;
using Xunit;

namespace QuizRush.Tests.GameSession
{
    public class GetSessionByCodeNotFoundTest
    {
        [Fact]
        public async Task GetSessionByCode_NonExistentCode_ReturnsNull()
        {
            var options = new DbContextOptionsBuilder<QuizRushDbContext>()
                .UseInMemoryDatabase(databaseName: "GetSessionByCodeNotFoundDb")
                .Options;
            using var context = new QuizRushDbContext(options);
            var service = new GameSessionService(context);

            var result = await service.GetSessionByCodeAsync("XXXXXX");

            Assert.Null(result);
        }
    }
}
