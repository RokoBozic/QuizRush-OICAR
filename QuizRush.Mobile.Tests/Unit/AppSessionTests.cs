using QuizRush.Core.ViewModels;
using QuizRush.Mobile.Services;

namespace QuizRush.Mobile.Tests.Unit;

public class AppSessionTests
{
    [Fact]
    public void Set_MarksSessionAuthenticated()
    {
        var session = new AppSession();
        session.Set(new AuthResponseViewModel
        {
            Token = "token",
            Username = "player1",
            Email = "p@test.com"
        });

        Assert.True(session.IsAuthenticated);
        Assert.Equal("player1", session.Username);
        Assert.Equal("p@test.com", session.Email);
    }

    [Fact]
    public void Clear_MarksSessionUnauthenticated()
    {
        var session = new AppSession();
        session.Set(new AuthResponseViewModel { Token = "token", Username = "u", Email = "e@t.com" });

        session.Clear();

        Assert.False(session.IsAuthenticated);
        Assert.Null(session.Token);
    }

    [Fact]
    public void SessionChanged_FiresOnSet()
    {
        var session = new AppSession();
        var fired = false;
        session.SessionChanged += () => fired = true;

        session.Set(new AuthResponseViewModel { Token = "t", Username = "u", Email = "e@t.com" });

        Assert.True(fired);
    }

    [Fact]
    public void Restore_PopulatesIdentityFromStorage()
    {
        var session = new AppSession();

        session.Restore("stored-token", "stored-user", "stored@email.com");

        Assert.Equal("stored-token", session.Token);
        Assert.Equal("stored-user", session.Username);
        Assert.Equal("stored@email.com", session.Email);
        Assert.True(session.IsAuthenticated);
    }

    [Fact]
    public void UpdateIdentity_UpdatesUsernameAndEmail()
    {
        var session = new AppSession();
        session.Set(new AuthResponseViewModel { Token = "t", Username = "old", Email = "old@t.com" });

        session.UpdateIdentity("new-name", "new@t.com");

        Assert.Equal("new-name", session.Username);
        Assert.Equal("new@t.com", session.Email);
        Assert.Equal("t", session.Token);
    }
}
