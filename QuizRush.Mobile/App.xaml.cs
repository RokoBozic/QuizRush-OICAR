namespace QuizRush.Mobile;

public partial class App : Application
{
    private readonly MobileRootPage _rootPage;

    public App(MobileRootPage rootPage)
    {
        InitializeComponent();
        _rootPage = rootPage;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(_rootPage);
    }
}
