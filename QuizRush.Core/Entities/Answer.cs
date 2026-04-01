using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuizRush.Core.Entities;

public class Answer
{
    public long Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public long QuestionId { get; set; }
    public Question Question { get; set; } = null!;
}
