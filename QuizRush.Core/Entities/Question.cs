using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuizRush.Core.Entities;

public class Question
{
    public long Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public long QuizId { get; set; }
    
    // Requirement: Points and Time limits for gameplay
    public int PointsValue { get; set; }
    public int TimeLimitSeconds { get; set; }

    public Quiz Quiz { get; set; } = null!;
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
}
