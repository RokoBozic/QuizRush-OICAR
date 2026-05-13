using QuizRush.Core.ViewModels;

namespace QuizRush.Core.Services
{
    public interface IUserService
    {
        Task<UserProfileViewModel?> GetProfileAsync(long userId);
        Task<PlayerStatsViewModel> GetPlayerStatsAsync(long userId);
        Task UpdateProfileAsync(long userId, UpdateProfileViewModel model);
        Task ChangePasswordAsync(long userId, ChangePasswordViewModel model);
    }
}
