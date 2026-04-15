using QuizRush.Core.ViewModels;

namespace QuizRush.Core.Services
{
    public interface IGameSessionService
    {
        Task<GameSessionViewModel> CreateSessionAsync(long quizId, long hostUserId);
        Task<GameSessionViewModel?> GetSessionByCodeAsync(string code);
        Task<IEnumerable<PlayerResultViewModel>> GetResultsAsync(long sessionId);
    }
}
