using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuizRush.Core.Entities;

public class User
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }

    //Points-based gambling system
    public int AccumulatedPoints { get; set; } = 0;

    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
}
