using System.ComponentModel.DataAnnotations;

namespace QuizRush.Core.ViewModels
{
    public class QuizViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, MinimumLength = 3)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "At least one question is required")]
        [MinLength(1, ErrorMessage = "At least one question is required")]
        public List<QuestionViewModel> Questions { get; set; } = new();
    }

    public class QuestionViewModel
    {
        [Required(ErrorMessage = "Question text is required")]
        [MaxLength(500)]
        public string Text { get; set; } = string.Empty;

        [Range(0, 1000)]
        public int PointsValue { get; set; }

        [Range(5, 600, ErrorMessage = "Time limit must be between 5 and 600 seconds")]
        public int TimeLimitSeconds { get; set; } = 20;

        [Required(ErrorMessage = "At least one answer is required")]
        [MinLength(2, ErrorMessage = "Each question must have at least two answers")]
        public List<AnswerViewModel> Answers { get; set; } = new();
    }

    public class AnswerViewModel
    {
        [Required(ErrorMessage = "Answer text is required")]
        [MinLength(1)]
        [MaxLength(250)]
        public string Text { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }
    }

    public class QuizResponseViewModel
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public long CreatorId { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<QuestionResponseViewModel> Questions { get; set; } = new();
    }

    public class QuestionResponseViewModel
    {
        public long Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public int PointsValue { get; set; }
        public int TimeLimitSeconds { get; set; }
        public List<AnswerResponseViewModel> Answers { get; set; } = new();
    }

    public class AnswerResponseViewModel
    {
        public long Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }
}