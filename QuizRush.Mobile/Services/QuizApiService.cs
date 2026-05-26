using System.Net;
using System.Net.Http.Json;
using QuizRush.Core.ViewModels;

namespace QuizRush.Mobile.Services;

public class QuizApiService
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;

    public QuizApiService(HttpClient httpClient, AuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
    }

    public async Task<List<QuizResponseViewModel>> GetMyQuizzesAsync()
    {
        _authService.ApplyAuthorizationHeader();
        return await _httpClient.GetFromJsonAsync<List<QuizResponseViewModel>>("api/quiz") ?? [];
    }

    public async Task<QuizResponseViewModel?> GetQuizByIdAsync(long quizId)
    {
        _authService.ApplyAuthorizationHeader();
        using var response = await _httpClient.GetAsync($"api/quiz/{quizId}");
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<QuizResponseViewModel>();
    }

    public async Task<QuizResponseViewModel> CreateQuizAsync(QuizViewModel model)
    {
        _authService.ApplyAuthorizationHeader();
        using var response = await _httpClient.PostAsJsonAsync("api/quiz", model);
        return await ReadQuizResponseAsync(response, "Could not create quiz.");
    }

    public async Task UpdateQuizAsync(long quizId, QuizViewModel model)
    {
        _authService.ApplyAuthorizationHeader();
        using var response = await _httpClient.PutAsJsonAsync($"api/quiz/{quizId}", model);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        throw new InvalidOperationException(await ReadErrorAsync(response, "Could not update quiz."));
    }

    private static async Task<QuizResponseViewModel> ReadQuizResponseAsync(HttpResponseMessage response, string fallback)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await ReadErrorAsync(response, fallback));
        }

        var quiz = await response.Content.ReadFromJsonAsync<QuizResponseViewModel>();
        if (quiz is null)
        {
            throw new InvalidOperationException(fallback);
        }

        return quiz;
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, string fallback)
    {
        var raw = await response.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(raw) ? fallback : raw.Trim('"');
    }
}
