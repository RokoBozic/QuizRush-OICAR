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
        public void ApplyGambling_CorrectAnswer_AddsGambleBonus()
        {
            var service = new ScoreCalculationService();

            int result = service.ApplyGambling(basePoints: 80, gamblingPercentage: 50, isCorrect: true);

            Assert.Equal(120, result);
        }

        [Fact]
        public void ApplyGambling_WrongAnswer_SubtractsGambleAmount()
        {
            var service = new ScoreCalculationService();

            int result = service.ApplyGambling(basePoints: 80, gamblingPercentage: 25, isCorrect: false);

            Assert.Equal(60, result);
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

            int result = service.ApplyGambling(basePoints: 150, gamblingPercentage: 0, isCorrect: true);

            Assert.Equal(150, result);
        }
    }
}
