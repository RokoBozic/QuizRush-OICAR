using Microsoft.EntityFrameworkCore;
using QuizRush.Infrastructure;
using QuizRush.Infrastructure.Services;
using Xunit;

namespace QuizRush.Tests.GameSession
{
    public class CreateSessionInvalidQuizTest
    {
        [Fact]
        public async Task CreateSession_NonExistentQuiz_ThrowsKeyNotFoundException()
        {
            var options = new DbContextOptionsBuilder<QuizRushDbContext>()
                .UseInMemoryDatabase(databaseName: "CreateSessionInvalidQuizDb")
                .Options;
            using var context = new QuizRushDbContext(options);
            var service = new GameSessionService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.CreateSessionAsync(quizId: 999, hostUserId: 1));
        }
    }
}
