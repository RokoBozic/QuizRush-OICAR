using Microsoft.EntityFrameworkCore;
using QuizRush.Core.Entities;
using QuizRush.Infrastructure;
using QuizRush.Infrastructure.Services;
using Xunit;

namespace QuizRush.Tests.GameSession
{
    public class CreateSessionValidTest
    {
        [Fact]
        public async Task CreateSession_ValidQuiz_ReturnsSessionWithCode()
        {
            var options = new DbContextOptionsBuilder<QuizRushDbContext>()
                .UseInMemoryDatabase(databaseName: "CreateSessionValidDb")
                .Options;
            using var context = new QuizRushDbContext(options);
            var service = new GameSessionService(context);

            var quiz = new QuizRush.Core.Entities.Quiz
            {
                Title = "Test Quiz",
                Description = "Test",
                CreatorId = 1
            };
            context.Quizzes.Add(quiz);
            await context.SaveChangesAsync();

            var result = await service.CreateSessionAsync(quiz.Id, hostUserId: 1);

            Assert.NotNull(result);
            Assert.Equal(6, result.Code.Length);
            Assert.Equal(quiz.Id, result.QuizId);
        }
    }
}
