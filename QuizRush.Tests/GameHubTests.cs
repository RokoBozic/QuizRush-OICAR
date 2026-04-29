using QuizRush.Infrastructure.Services;

namespace QuizRush.Tests
{
    public class GameHubTests
    {
        [Fact]
        public void CalculatePoints_CorrectAnswer_AppliesTimePenalty()
        {
            var service = new ScoreCalculationService();

            int result = service.CalculatePoints(basePoints: 100, timeToAnswerSeconds: 10, isCorrect: true);

            Assert.Equal(70, result);
        }

        [Fact]
        public void CalculatePoints_WrongAnswer_ReturnsZero()
        {
            var service = new ScoreCalculationService();

            int result = service.CalculatePoints(basePoints: 100, timeToAnswerSeconds: 2, isCorrect: false);

            Assert.Equal(0, result);
        }

        [Fact]
        public void ApplyGambling_CorrectAnswer_AddsStakeFromPlayerScore()
        {
            var service = new ScoreCalculationService();

            // 100 total score, 50% stake = 50; earned 80 → 80 + 50 = 130 delta
            int result = service.ApplyGambling(earnedQuestionPoints: 80, playerScoreBeforeAnswer: 100, gamblingPercentage: 50, isCorrect: true);

            Assert.Equal(130, result);
        }

        [Fact]
        public void ApplyGambling_WrongAnswer_LosesStakeFromPlayerScore()
        {
            var service = new ScoreCalculationService();

            // 100 score, 50% stake = 50 lost (earned points are 0 when wrong)
            int result = service.ApplyGambling(earnedQuestionPoints: 0, playerScoreBeforeAnswer: 100, gamblingPercentage: 50, isCorrect: false);

            Assert.Equal(-50, result);
        }

        [Fact]
        public void ApplyGambling_100Points50PercentWin_NoQuestionPoints_Reaches150PercentOfStartingScore()
        {
            var service = new ScoreCalculationService();

            int delta = service.ApplyGambling(earnedQuestionPoints: 0, playerScoreBeforeAnswer: 100, gamblingPercentage: 50, isCorrect: true);

            Assert.Equal(50, delta);
            Assert.Equal(150, 100 + delta);
        }

        [Fact]
        public void ApplyGambling_100Points50PercentWrong_LeavesHalfOfStartingScore()
        {
            var service = new ScoreCalculationService();

            int delta = service.ApplyGambling(earnedQuestionPoints: 0, playerScoreBeforeAnswer: 100, gamblingPercentage: 50, isCorrect: false);

            Assert.Equal(-50, delta);
            Assert.Equal(50, 100 + delta);
        }

        [Fact]
        public void CalculatePoints_NeverDropsBelowZero()
        {
            var service = new ScoreCalculationService();

            int result = service.CalculatePoints(basePoints: 100, timeToAnswerSeconds: 100, isCorrect: true);

            Assert.Equal(0, result);
        }

        [Fact]
        public void ApplyGambling_ZeroPercentage_LeavesScoreUnchanged()
        {
            var service = new ScoreCalculationService();

            int result = service.ApplyGambling(earnedQuestionPoints: 150, playerScoreBeforeAnswer: 999, gamblingPercentage: 0, isCorrect: true);

            Assert.Equal(150, result);
        }
    }
}
