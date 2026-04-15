using System.ComponentModel.DataAnnotations;

namespace QuizRush.Core.ViewModels
{
    public class QuizViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, MinimumLength = 3)]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "At least one question is required")]
        public List<QuestionViewModel> Questions { get; set; } = new();
    }

    public class QuestionViewModel
    {
        [Required(ErrorMessage = "Question text is required")]
        [MaxLength(500)]
        public string Text { get; set; } = string.Empty;

        [Range(0, 1000)]
        public int PointsValue { get; set; }

        public int TimeLimitSeconds { get; set; } = 20;

        [Required(ErrorMessage = "At least one answer is required")]
        public List<AnswerViewModel> Answers { get; set; } = new();
    }

    public class AnswerViewModel
    {
        [Required(ErrorMessage = "Answer text is required")]
        [MaxLength(250)]
        public string Text { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }
    }
}