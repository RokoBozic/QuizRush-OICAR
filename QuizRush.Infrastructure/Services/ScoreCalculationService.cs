namespace QuizRush.Infrastructure.Services
{
    public class ScoreCalculationService
    {
        private const double PenaltyPerSecond = 0.03;

        public int CalculatePoints(int basePoints, int timeToAnswerSeconds, bool isCorrect)
        {
            if (!isCorrect)
            {
                return 0;
            }

            double multiplier = 1.0 - (timeToAnswerSeconds * PenaltyPerSecond);
            int earned = (int)(basePoints * Math.Max(multiplier, 0.0));
            return Math.Max(earned, 0);
        }

        public int ApplyGambling(int basePoints, int gamblingPercentage, bool isCorrect)
        {
            if (gamblingPercentage <= 0)
            {
                return basePoints;
            }

            int gamblingAmount = (int)(basePoints * (gamblingPercentage / 100.0));
            return isCorrect
                ? basePoints + gamblingAmount
                : basePoints - gamblingAmount;
        }
    }
}
