using QuizRush.Web.Components;

var builder = WebApplication.CreateBuilder(args);

string apiBaseUrl = (builder.Configuration["QuizRush:ApiBaseUrl"] ?? "http://localhost:5176").TrimEnd('/') + "/";

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
});
builder.Services.AddScoped<QuizRush.Web.Services.GameService>();
builder.Services.AddScoped<QuizRush.Web.Services.AuthService>();
builder.Services.AddScoped<QuizRush.Web.Services.LocalStorageService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseAntiforgery();
app.UseStaticFiles();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
