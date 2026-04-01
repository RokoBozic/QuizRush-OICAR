using System.ComponentModel.DataAnnotations;

namespace QuizRush.Core.Entities;

public class Quiz
{
    [Key]
    public long Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public long CreatorId { get; set; }

    public User Creator { get; set; } = null!;
    public ICollection<Question> Questions { get; set; } = new List<Question>();
}
