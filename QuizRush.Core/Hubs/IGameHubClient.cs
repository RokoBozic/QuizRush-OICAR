namespace QuizRush.Core.Hubs
{
    public interface IGameHubClient
    {
        // Host calls
        Task HostGame(long quizId);
        Task StartGame(string sessionCode);
        Task NextQuestion(string sessionCode);
        Task EndGame(string sessionCode);

        // Player calls
        Task JoinGame(string sessionCode, string playerName);
        Task SubmitAnswer(string sessionCode, long answerId);
        Task PlaceGamble(string sessionCode, int gamblingPercentage);
        Task LeaveGame(string sessionCode);
    }
}
