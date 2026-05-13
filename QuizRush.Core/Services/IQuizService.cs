using QuizRush.Core.Entities;
using QuizRush.Core.ViewModels;

namespace QuizRush.Core.Services
{
    public interface IQuizService
    {
        Task<IEnumerable<Quiz>> GetAllForCreatorAsync(long creatorId);
        Task<Quiz?> GetByIdForCreatorAsync(long id, long creatorId);
        Task<Quiz> CreateAsync(QuizViewModel model, long creatorId);
        Task UpdateAsync(long id, QuizViewModel model, long actingUserId);
        Task DeleteAsync(long id, long actingUserId);
    }
}