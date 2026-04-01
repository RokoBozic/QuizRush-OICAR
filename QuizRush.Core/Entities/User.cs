using System.ComponentModel.DataAnnotations;

namespace QuizRush.Core.Entities;

public class User
{
    [Key]
    public long Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress] 
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsAdmin { get; set; } = false;

    public int AccumulatedPoints { get; set; } = 0;

    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
}