using System.Text.Json.Serialization;

namespace QuizRush.Core.ViewModels
{
    public class QuestionData
    {
        [JsonPropertyName("questionId")]
        public long QuestionId { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("pointsValue")]
        public int PointsValue { get; set; }

        [JsonPropertyName("timeLimit")]
        public int TimeLimit { get; set; }

        [JsonPropertyName("answers")]
        public List<AnswerOptionData> Answers { get; set; } = new();
    }

    public class AnswerOptionData
    {
        [JsonPropertyName("answerId")]
        public long AnswerId { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    public class AnswerData
    {
        public long AnswerId { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int BasePoints { get; set; }
        public int GamblingBonus { get; set; }
        public int TotalPoints { get; set; }
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
