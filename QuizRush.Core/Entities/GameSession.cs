using System.ComponentModel.DataAnnotations;

namespace QuizRush.Core.Entities
{
    public class GameSession
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [MaxLength(6)]
        public string Code { get; set; } = string.Empty;

        [Required]
        public long QuizId { get; set; }

        [Required]
        public long HostUserId { get; set; }

        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; }

        public GameStatus Status { get; set; } = GameStatus.WaitingToStart;

        public Quiz Quiz { get; set; } = null!;
        public User HostUser { get; set; } = null!;
        public ICollection<Player> Players { get; set; } = new List<Player>();
        public ICollection<PlayerAnswer> PlayerAnswers { get; set; } = new List<PlayerAnswer>();
    }

    public enum GameStatus
    {
        WaitingToStart,
        InProgress,
        Completed
    }
}
