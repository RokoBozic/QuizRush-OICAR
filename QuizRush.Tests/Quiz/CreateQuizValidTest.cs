using Microsoft.EntityFrameworkCore;
using QuizRush.Core.ViewModels;
using QuizRush.Infrastructure;
using QuizRush.Infrastructure.Services;
using Xunit;

namespace QuizRush.Tests.Quiz
{
    public class CreateQuizValidTest
    {
        [Fact]
        public async Task CreateQuiz_ValidData_ReturnsCreatedQuiz()
        {
            var options = new DbContextOptionsBuilder<QuizRushDbContext>()
                .UseInMemoryDatabase(databaseName: "CreateQuizValidDb")
                .Options;
            using var context = new QuizRushDbContext(options);
            var service = new QuizService(context);

            var model = new QuizViewModel
            {
                Title = "Sample Quiz",
                Description = "A test quiz",
                Questions = new List<QuestionViewModel>
                {
                    new QuestionViewModel
                    {
                        Text = "What is 2+2?",
                        PointsValue = 100,
                        TimeLimitSeconds = 20,
                        Answers = new List<AnswerViewModel>
                        {
                            new AnswerViewModel { Text = "4", IsCorrect = true },
                            new AnswerViewModel { Text = "3", IsCorrect = false }
                        }
                    }
                }
            };

            var result = await service.CreateAsync(model, creatorId: 1);

            Assert.Equal("Sample Quiz", result.Title);
            Assert.Single(result.Questions);
        }
    }
}
