using Microsoft.EntityFrameworkCore;
using QuizRush.Core.ViewModels;
using QuizRush.Infrastructure;
using QuizRush.Infrastructure.Services;
using Xunit;

namespace QuizRush.Tests.Quiz
{
    public class CreateQuizNoCorrectAnswerTest
    {
        [Fact]
        public async Task CreateQuiz_NoCorrectAnswer_ThrowsArgumentException()
        {
            var options = new DbContextOptionsBuilder<QuizRushDbContext>()
                .UseInMemoryDatabase(databaseName: "CreateQuizNoCorrectAnswerDb")
                .Options;
            using var context = new QuizRushDbContext(options);
            var service = new QuizService(context);

            var model = new QuizViewModel
            {
                Title = "Bad Quiz",
                Description = "Question with no correct answer",
                Questions = new List<QuestionViewModel>
                {
                    new QuestionViewModel
                    {
                        Text = "What is 2+2?",
                        PointsValue = 100,
                        TimeLimitSeconds = 20,
                        Answers = new List<AnswerViewModel>
                        {
                            new AnswerViewModel { Text = "3", IsCorrect = false },
                            new AnswerViewModel { Text = "5", IsCorrect = false }
                        }
                    }
                }
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.CreateAsync(model, creatorId: 1));
        }
    }
}
