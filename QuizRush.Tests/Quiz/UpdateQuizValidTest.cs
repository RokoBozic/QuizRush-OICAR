using Microsoft.EntityFrameworkCore;
using QuizRush.Core.ViewModels;
using QuizRush.Infrastructure;
using QuizRush.Infrastructure.Services;
using Xunit;

namespace QuizRush.Tests.Quiz
{
    public class UpdateQuizValidTest
    {
        [Fact]
        public async Task UpdateQuiz_ValidData_UpdatesQuiz()
        {
            var options = new DbContextOptionsBuilder<QuizRushDbContext>()
                .UseInMemoryDatabase(databaseName: "UpdateQuizValidDb")
                .Options;
            using var context = new QuizRushDbContext(options);
            var service = new QuizService(context);

            var createModel = new QuizViewModel
            {
                Title = "Original Title",
                Description = "Original Description",
                Questions = new List<QuestionViewModel>
                {
                    new QuestionViewModel
                    {
                        Text = "Original question?",
                        PointsValue = 100,
                        TimeLimitSeconds = 20,
                        Answers = new List<AnswerViewModel>
                        {
                            new AnswerViewModel { Text = "Correct", IsCorrect = true },
                            new AnswerViewModel { Text = "Wrong", IsCorrect = false }
                        }
                    }
                }
            };

            var created = await service.CreateAsync(createModel, creatorId: 1);

            var updateModel = new QuizViewModel
            {
                Title = "Updated Title",
                Description = "Updated Description",
                Questions = new List<QuestionViewModel>
                {
                    new QuestionViewModel
                    {
                        Text = "Updated question?",
                        PointsValue = 200,
                        TimeLimitSeconds = 30,
                        Answers = new List<AnswerViewModel>
                        {
                            new AnswerViewModel { Text = "New correct", IsCorrect = true },
                            new AnswerViewModel { Text = "New wrong", IsCorrect = false }
                        }
                    }
                }
            };

            await service.UpdateAsync(created.Id, updateModel, actingUserId: 1);

            var updated = await service.GetByIdForCreatorAsync(created.Id, 1);
            Assert.Equal("Updated Title", updated!.Title);
            Assert.Equal("Updated question?", updated.Questions.First().Text);
        }
    }
}
