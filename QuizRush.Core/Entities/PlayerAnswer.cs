using System.ComponentModel.DataAnnotations;

namespace QuizRush.Core.Entities
{
    public class PlayerAnswer
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long GameSessionId { get; set; }

        [Required]
        public long PlayerId { get; set; }
        [Required]
        public long QuestionId { get; set; }
        [Required]
        public long AnswerId { get; set; }

        public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
        public int ScoreEarned { get; set; }
        public TimeSpan ResponseTime { get; set; }

        public GameSession GameSession { get; set; } = null!;
        public Player Player { get; set; } = null!;
        public Question Question { get; set; } = null!;
        public Answer Answer { get; set; } = null!;
    }
}