using Microsoft.EntityFrameworkCore;
using QuizRush.Core.ViewModels;
using QuizRush.Infrastructure;
using QuizRush.Infrastructure.Services;
using Xunit;

namespace QuizRush.Tests.Quiz
{
    public class DeleteQuizValidTest
    {
        [Fact]
        public async Task DeleteQuiz_ExistingId_RemovesQuiz()
        {
            var options = new DbContextOptionsBuilder<QuizRushDbContext>()
                .UseInMemoryDatabase(databaseName: "DeleteQuizValidDb")
                .Options;
            using var context = new QuizRushDbContext(options);
            var service = new QuizService(context);

            var model = new QuizViewModel
            {
                Title = "Quiz to delete",
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

            var created = await service.CreateAsync(model, creatorId: 1);

            await service.DeleteAsync(created.Id);

            var result = await service.GetByIdAsync(created.Id);
            Assert.Null(result);
        }
    }
}
