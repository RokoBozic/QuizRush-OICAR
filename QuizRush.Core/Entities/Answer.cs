using System.ComponentModel.DataAnnotations;

namespace QuizRush.Core.Entities;

public class Answer
{
    [Key]
    public long Id { get; set; }

    [Required]
    [MaxLength(250)]
    public string Text { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }

    [Required]
    public long QuestionId { get; set; }

    public Question Question { get; set; } = null!;
}