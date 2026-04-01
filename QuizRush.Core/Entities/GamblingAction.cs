

using System.ComponentModel.DataAnnotations;

namespace QuizRush.Core.Entities
{
    public class GamblingAction
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long PlayerId { get; set; }
        [Required]
        public long GameSessionId { get; set; }

        public int PointsGambled { get; set; }
        public bool Won { get; set; }

        public Player Player { get; set; } = null!;
        public Question Question { get; set; } = null!;
        public GameSession GameSession { get; set; } = null!;

    }
}
