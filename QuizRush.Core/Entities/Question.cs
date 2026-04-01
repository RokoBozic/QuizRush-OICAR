using System.ComponentModel.DataAnnotations;

namespace QuizRush.Core.Entities;

public class Question
{
    [Key]
    public long Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string Text { get; set; } = string.Empty;

    [Required]
    public long QuizId { get; set; }


    public int PointsValue { get; set; }


    public int TimeLimitSeconds { get; set; }

    public Quiz Quiz { get; set; } = null!;
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
}