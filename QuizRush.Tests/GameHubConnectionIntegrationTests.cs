using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using QuizRush.Api;

namespace QuizRush.Tests;

/// <summary>
/// Lightweight in-process checks for the API host and SignalR negotiation.
/// For full play-through validation, run two browsers: host on /host, player on /join then /play.
/// </summary>
public class GameHubConnectionIntegrationTests
{
    [Fact]
    public async Task SwaggerJson_IsAvailable()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting(WebHostDefaults.EnvironmentKey, Environments.Development));

        var client = factory.CreateClient();
        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GameHub_AcceptsAnonymousConnection()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting(WebHostDefaults.EnvironmentKey, Environments.Development));

        var client = factory.CreateClient();
        var baseUri = client.BaseAddress ?? throw new InvalidOperationException("Missing BaseAddress.");
        var hubUri = new Uri(baseUri, "hub/game");

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUri, options => options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler())
            .Build();

        await connection.StartAsync();
        try
        {
            Assert.Equal(HubConnectionState.Connected, connection.State);
        }
        finally
        {
            await connection.StopAsync();
        }
    }
}
