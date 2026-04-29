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

        /// <summary>
        /// Applies gamble as a stake from the player's current score: stake = score * (gamble% / 100).
        /// Correct: earn question points plus the stake back as a bonus (e.g. 100 pts, 50% → +50 on top of earned).
        /// Wrong: lose the stake (e.g. 100 pts, 50% → −50, leaving 50% of the pre-answer score from that loss alone).
        /// </summary>
        /// <param name="earnedQuestionPoints">Points from the question (time/correctness), before gambling.</param>
        /// <param name="playerScoreBeforeAnswer">Player total score before this answer is applied.</param>
        public int ApplyGambling(int earnedQuestionPoints, int playerScoreBeforeAnswer, int gamblingPercentage, bool isCorrect)
        {
            if (gamblingPercentage <= 0)
            {
                return earnedQuestionPoints;
            }

            int stake = (int)(playerScoreBeforeAnswer * (gamblingPercentage / 100.0));
            return isCorrect
                ? earnedQuestionPoints + stake
                : -stake;
        }
    }
}
