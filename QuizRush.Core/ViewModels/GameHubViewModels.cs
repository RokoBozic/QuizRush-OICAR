namespace QuizRush.Core.ViewModels
{
    public class QuestionData
    {
        public long QuestionId { get; set; }
        public string Text { get; set; } = string.Empty;
        public int PointsValue { get; set; }
        public int TimeLimit { get; set; }
        public List<AnswerOptionData> Answers { get; set; } = new();
    }

    public class AnswerOptionData
    {
        public long AnswerId { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    public class AnswerData
    {
        public long AnswerId { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }

    public class LeaderboardData
    {
        public string PlayerName { get; set; } = string.Empty;
        public int Score { get; set; }
        public int Rank { get; set; }
    }

    public class PlayerAnswerSubmissionData
    {
        public long PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public long AnswerId { get; set; }
        public long QuestionId { get; set; }
        public int TimeToAnswerSeconds { get; set; }
        public bool IsCorrect { get; set; }
        public int PointsEarned { get; set; }
    }
}
