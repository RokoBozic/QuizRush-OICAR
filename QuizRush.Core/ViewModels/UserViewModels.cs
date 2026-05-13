using System.ComponentModel.DataAnnotations;

namespace QuizRush.Core.ViewModels
{
    public class UserProfileViewModel
    {
        public long Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int AccumulatedPoints { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateProfileViewModel
    {
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;
    }

    public class ChangePasswordViewModel
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class PlayerGameHistoryItemViewModel
    {
        public string SessionCode { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int Score { get; set; }
        public int Rank { get; set; }
    }

    public class PlayerStatsViewModel
    {
        public int AccumulatedPoints { get; set; }
        public int GamesPlayed { get; set; }
        public int GamesWon { get; set; }
        public int HighestScore { get; set; }
        public List<PlayerGameHistoryItemViewModel> GameHistory { get; set; } = new();
    }
}
