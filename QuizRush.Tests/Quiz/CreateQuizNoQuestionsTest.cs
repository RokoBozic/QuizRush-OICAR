using Microsoft.EntityFrameworkCore;
using QuizRush.Core.ViewModels;
using QuizRush.Infrastructure;
using QuizRush.Infrastructure.Services;
using Xunit;

namespace QuizRush.Tests.Quiz
{
    public class CreateQuizNoQuestionsTest
    {
        [Fact]
        public async Task CreateQuiz_NoQuestions_ThrowsArgumentException()
        {
            var options = new DbContextOptionsBuilder<QuizRushDbContext>()
                .UseInMemoryDatabase(databaseName: "CreateQuizNoQuestionsDb")
                .Options;
            using var context = new QuizRushDbContext(options);
            var service = new QuizService(context);

            var model = new QuizViewModel
            {
                Title = "Empty Quiz",
                Description = "No questions",
                Questions = new List<QuestionViewModel>()
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.CreateAsync(model, creatorId: 1));
        }
    }
}
