namespace QuizRush.Mobile;

public class MobileRootPage : TabbedPage
{
    public MobileRootPage(MainPage playPage, Pages.HostPage hostPage, Pages.QuizzesPage quizzesPage, Pages.AccountPage accountPage)
    {
        Title = "QuizRush";
        Children.Add(new NavigationPage(playPage) { Title = "Play" });
        Children.Add(new NavigationPage(hostPage) { Title = "Host" });
        Children.Add(new NavigationPage(quizzesPage) { Title = "Quizzes" });
        Children.Add(new NavigationPage(accountPage) { Title = "Account" });
    }
}
