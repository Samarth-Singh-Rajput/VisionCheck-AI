using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using VisionCheckAI.Client;
using VisionCheckAI.Client.Services;
using VisionCheckAI.Client.Services.Fakes;
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
var apiBaseUrl = builder.Configuration["Api:BaseUrl"];
if (string.IsNullOrWhiteSpace(apiBaseUrl))
{
    apiBaseUrl = builder.HostEnvironment.BaseAddress;
}

var apiSettings = new ApiSettings { BaseUrl = apiBaseUrl };
builder.Services.AddSingleton(apiSettings);
builder.Services.AddSingleton<BrowserStorage>();
builder.Services.AddSingleton<SessionStore>();
builder.Services.AddSingleton<ThemeService>();
builder.Services.AddSingleton<ToastService>();
builder.Services.AddScoped<AuthTokenHandler>();
builder.Services
    .AddHttpClient(ApiSettings.HttpClientName, client =>
    {
        client.BaseAddress = new Uri(apiSettings.BaseUrl);
        client.Timeout = TimeSpan.FromMinutes(2);
    })
    .AddHttpMessageHandler<AuthTokenHandler>();
var useFakeData = builder.Configuration.GetValue<bool?>("Api:UseFakeData") ?? true;
if (useFakeData)
{
    builder.Services.AddScoped<IAuthApi, FakeAuthApi>();
    builder.Services.AddScoped<IProductApi, FakeProductApi>();
    builder.Services.AddScoped<IInspectionApi, FakeInspectionApi>();
    builder.Services.AddScoped<IDashboardApi, FakeDashboardApi>();
}
else
{
    builder.Services.AddScoped<IAuthApi, AuthApi>();
    builder.Services.AddScoped<IProductApi, ProductApi>();
    builder.Services.AddScoped<IInspectionApi, InspectionApi>();
    builder.Services.AddScoped<IDashboardApi, DashboardApi>();
}
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, VisionCheckAuthStateProvider>();

var host = builder.Build();

// Restore theme and session before the first render so the shell does not flash.
await host.Services.GetRequiredService<ThemeService>().InitializeAsync();
await host.Services.GetRequiredService<SessionStore>().InitializeAsync();

await host.RunAsync();
