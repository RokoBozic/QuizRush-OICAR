using QuizRush.Core.Entities;
using QuizRush.Core.ViewModels;

namespace QuizRush.Core.Services
{
    public interface IQuizService
    {
        Task<IEnumerable<Quiz>> GetAllAsync();
        Task<Quiz?> GetByIdAsync(long id);
        Task<Quiz> CreateAsync(QuizViewModel model, long creatorId);
        Task UpdateAsync(long id, QuizViewModel model);
        Task DeleteAsync(long id);
    }
}