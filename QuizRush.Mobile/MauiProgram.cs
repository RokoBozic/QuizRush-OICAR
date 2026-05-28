using Microsoft.Extensions.Logging;
using QuizRush.Mobile.Services;

namespace QuizRush.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<ApiEndpointProvider>();
        builder.Services.AddSingleton(sp =>
        {
            var endpoints = sp.GetRequiredService<ApiEndpointProvider>();
            return new HttpClient
            {
                BaseAddress = new Uri(endpoints.GetBaseUrl())
            };
        });
        builder.Services.AddSingleton<AuthStorageService>();
        builder.Services.AddSingleton<AppSession>();
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<QuizApiService>();
        builder.Services.AddSingleton<UserApiService>();
        builder.Services.AddSingleton<PlayerGameService>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<Pages.HostPage>();
        builder.Services.AddTransient<Pages.QuizzesPage>();
        builder.Services.AddTransient<Pages.AccountPage>();
        builder.Services.AddSingleton<MobileRootPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
