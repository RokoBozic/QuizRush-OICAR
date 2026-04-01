using System.ComponentModel.DataAnnotations;

namespace QuizRush.Core.Entities
{
    public class Player
    {
        [Key]
        public long Id { get; set; }
        [Required]
        public long GameSessionId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        public int Score { get; set; } = 0;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public long? UserId { get; set; }

        public GameSession GameSession { get; set; } = null!;
        public User? User { get; set; }
    }
}