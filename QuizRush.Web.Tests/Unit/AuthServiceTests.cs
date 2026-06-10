using System.Net;
using System.Net.Http.Json;
using QuizRush.Core.ViewModels;
using QuizRush.Web.Services;

namespace QuizRush.Web.Tests.Unit;

public class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_OnSuccess_StoresTokenAndReturnsAuth()
    {
        var storage = new FakeLocalStorageService();
        var auth = new AuthResponseViewModel { Token = "jwt-token", Username = "alice", Email = "a@test.com" };
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(auth)
        });
        var service = new AuthService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, storage);

        var result = await service.LoginAsync("a@test.com", "password");

        Assert.NotNull(result);
        Assert.Equal("jwt-token", await storage.GetItemAsync("token"));
    }

    [Fact]
    public async Task LoginAsync_OnFailure_ReturnsNull()
    {
        var storage = new FakeLocalStorageService();
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var service = new AuthService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, storage);

        var result = await service.LoginAsync("a@test.com", "wrong");

        Assert.Null(result);
        Assert.Null(await storage.GetItemAsync("token"));
    }

    [Fact]
    public async Task LogoutAsync_ClearsStoredToken()
    {
        var storage = new FakeLocalStorageService();
        await storage.SetItemAsync("token", "existing");
        var service = new AuthService(new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)))
        {
            BaseAddress = new Uri("http://localhost/")
        }, storage);

        await service.LogoutAsync();

        Assert.Null(await storage.GetItemAsync("token"));
    }

    [Fact]
    public async Task IsAuthenticatedAsync_ReturnsFalseWhenNoToken()
    {
        var service = new AuthService(
            new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)))
            {
                BaseAddress = new Uri("http://localhost/")
            },
            new FakeLocalStorageService());

        Assert.False(await service.IsAuthenticatedAsync());
    }

    [Fact]
    public async Task RegisterAsync_OnSuccess_ReturnsMessage()
    {
        var storage = new FakeLocalStorageService();
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("\"Registration successful.\"")
        });
        var service = new AuthService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, storage);

        var result = await service.RegisterAsync("newuser", "new@test.com", "Password1!");

        Assert.Equal("\"Registration successful.\"", result);
    }
}

internal sealed class FakeLocalStorageService : LocalStorageService
{
    private readonly Dictionary<string, string> _store = new();

    public FakeLocalStorageService() : base(new NoOpJsRuntime()) { }

    public override Task<string?> GetItemAsync(string key) =>
        Task.FromResult(_store.TryGetValue(key, out var value) ? value : null);

    public override Task SetItemAsync(string key, string value)
    {
        _store[key] = value;
        return Task.CompletedTask;
    }

    public override Task RemoveItemAsync(string key)
    {
        _store.Remove(key);
        return Task.CompletedTask;
    }
}

internal sealed class NoOpJsRuntime : Microsoft.JSInterop.IJSRuntime
{
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
        default;

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, System.Threading.CancellationToken cancellationToken, object?[]? args) =>
        default;
}

internal sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(responder(request));
}
