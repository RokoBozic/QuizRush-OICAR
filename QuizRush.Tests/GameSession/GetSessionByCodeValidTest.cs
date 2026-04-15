using Microsoft.EntityFrameworkCore;
using QuizRush.Core.Entities;
using QuizRush.Infrastructure;
using QuizRush.Infrastructure.Services;
using Xunit;

namespace QuizRush.Tests.GameSession
{
    public class GetSessionByCodeValidTest
    {
        [Fact]
        public async Task GetSessionByCode_ExistingCode_ReturnsSession()
        {
            var options = new DbContextOptionsBuilder<QuizRushDbContext>()
                .UseInMemoryDatabase(databaseName: "GetSessionByCodeValidDb")
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

            var created = await service.CreateSessionAsync(quiz.Id, hostUserId: 1);

            var result = await service.GetSessionByCodeAsync(created.Code);

            Assert.NotNull(result);
            Assert.Equal(created.Code, result.Code);
            Assert.Equal(quiz.Id, result.QuizId);
        }
    }
}
