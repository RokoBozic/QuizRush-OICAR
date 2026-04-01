using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuizRush.Core.Entities;

public class Quiz
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public long CreatorId { get; set; }
    public User Creator { get; set; } = null!;
    public ICollection<Question> Questions { get; set; } = new List<Question>();
}
