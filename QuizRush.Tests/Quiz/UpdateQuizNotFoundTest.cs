using Microsoft.EntityFrameworkCore;
using QuizRush.Core.ViewModels;
using QuizRush.Infrastructure;
using QuizRush.Infrastructure.Services;
using Xunit;

namespace QuizRush.Tests.Quiz
{
    public class UpdateQuizNotFoundTest
    {
        [Fact]
        public async Task UpdateQuiz_NonExistentId_ThrowsKeyNotFoundException()
        {
            var options = new DbContextOptionsBuilder<QuizRushDbContext>()
                .UseInMemoryDatabase(databaseName: "UpdateQuizNotFoundDb")
                .Options;
            using var context = new QuizRushDbContext(options);
            var service = new QuizService(context);

            var model = new QuizViewModel
            {
                Title = "Doesn't matter",
                Questions = new List<QuestionViewModel>
                {
                    new QuestionViewModel
                    {
                        Text = "Some question?",
                        Answers = new List<AnswerViewModel>
                        {
                            new AnswerViewModel { Text = "Answer", IsCorrect = true }
                        }
                    }
                }
            };

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.UpdateAsync(999, model));
        }
    }
}
