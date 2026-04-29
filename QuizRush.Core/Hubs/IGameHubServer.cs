using QuizRush.Core.ViewModels;

namespace QuizRush.Core.Hubs
{
    public interface IGameHubServer
    {
        // Updates sent to host
        Task PlayerJoined(string playerName, int totalPlayers);
        Task PlayerLeft(string playerName, int remainingPlayers);
        Task GameStarted(int questionCount);
        Task QuestionDisplayed(int questionNumber, int timeLimit);
        Task QuestionAnswered(int playersAnswered, int totalPlayers);
        Task AllPlayersAnswered();

        // Updates sent to all players
        Task GameJoined(string sessionCode, int totalPlayers, string hostName);
        Task QuestionReady(QuestionData question, int timeLimit);
        Task SubmissionPhaseEnded();
        Task GamblingPhaseStarted(int secondsRemaining);
        Task AnswerRevealed(AnswerData correctAnswer);
        Task ScoresUpdated(LeaderboardData[] leaderboard);
        Task GameEnded(LeaderboardData[] finalLeaderboard);
        Task GameError(string errorMessage);
        Task SessionExpired();

        /// <summary>Confirmation to the host only after a host control action.</summary>
        Task HostSelfAck(string message);

        /// <summary>Shown to players (not the host) when the host advances or ends the game.</summary>
        Task HostPlayerNotice(string message);
    }
}
