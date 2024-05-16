using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using HemoStat.Client;
using Microsoft.AspNetCore.Components.Authorization;
using HemoStat.Client.Services;


var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddDevExpressBlazor(options => {
    options.BootstrapVersion = DevExpress.Blazor.BootstrapVersion.v5;
    options.SizeMode = DevExpress.Blazor.SizeMode.Medium;
});
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddSingleton<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();
builder.Services.AddScoped<HostingEnvironmentService>();


builder.Services
    .AddTransient<CookieHandler>()
    .AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("API"))
    .AddHttpClient("API", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<CookieHandler>();


await builder.Build().RunAsync();