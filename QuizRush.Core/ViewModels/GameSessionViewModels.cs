using QuizRush.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace QuizRush.Core.ViewModels
{
    public class CreateSessionViewModel
    {
        [Range(1, long.MaxValue, ErrorMessage = "QuizId must be a positive number.")]
        public long QuizId { get; set; }
    }

    public class GameSessionViewModel
    {
        public long Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public long QuizId { get; set; }
        public string QuizTitle { get; set; } = string.Empty;
        public GameStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public int PlayerCount { get; set; }
    }

    public class PlayerResultViewModel
    {
        public string PlayerName { get; set; } = string.Empty;
        public int Score { get; set; }
        public int Rank { get; set; }
    }
}
