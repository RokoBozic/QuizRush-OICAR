namespace QuizRush.Mobile;

public partial class AppShell : Shell
{
    public AppShell(MainPage mainPage)
    {
        InitializeComponent();

        Items.Add(new ShellContent
        {
            Title = "Play",
            Content = mainPage,
            Route = nameof(MainPage)
        });
    }
}
