using HemoStat.Components;
using HemoStat.Services;
using HemoStat.Client.Pages;
using HemoStat.Components.Account;
using HemoStat.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HemoStat.Configuration;
using Microsoft.AspNetCore.Authorization;
using HemoStat.Client.Services;
using HemoStat.Client;
using Serilog;
using HemoStat.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Add Swagger Configuration
builder.AddStandardServices();

builder.Services.AddDevExpressBlazor(options => {
    options.BootstrapVersion = DevExpress.Blazor.BootstrapVersion.v5;
    options.SizeMode = DevExpress.Blazor.SizeMode.Medium;
});
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddMvc();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, PersistingRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthorization();

builder.Services.AddAuthentication(options => {
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
    .AddIdentityCookies();

builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, BlazorAuthorizationMiddlewareResultHandler>();
builder.Services.AddScoped<HostingEnvironmentService>();
builder.Services.AddSingleton<BaseUrlProvider>();

builder.Services
    .AddTransient<CookieHandler>()
    .AddScoped(sp => sp
    .GetRequiredService<IHttpClientFactory>()
    .CreateClient("API"))
    .AddHttpClient("API", (provider, client) =>
    {
        // Get base address
        var uri = provider.GetRequiredService<BaseUrlProvider>().BaseUrl;
        client.BaseAddress = new Uri(uri);
    }).AddHttpMessageHandler<CookieHandler>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Migrations and Database Setup - can comment out after database is created
// Temporary build a service provider for running database migrations
var tempServiceProvider = builder.Services.BuildServiceProvider();

// Create a scope to get the service provider
//using (var scope = tempServiceProvider.CreateScope())
//{
//    var services = scope.ServiceProvider;
//    var dbContext = services.GetRequiredService<ApplicationDbContext>();
//    dbContext.Database.Migrate();  // This applies pending migrations or creates the database if it does not exist
//}

builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddHttpContextAccessor();
builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddRoleManager<RoleManager<IdentityRole>>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Host.UseSerilog((context, config) =>
{
    config.Enrich.FromLogContext();
    config.ReadFrom.Configuration(context.Configuration);
});

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
builder.Services.AddBrowserTimeProvider();

var app = builder.Build();

// Configure the HTTP request pipeline.
if(app.Environment.IsDevelopment()) {
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();

    // Include Swagger if Development
    app.UseSwagger();
    app.UseSwaggerUI();

} else {
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.ConfigurePingApi();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(HemoStat.Client._Imports).Assembly);

app.MapAdditionalIdentityEndpoints();

// Seeding the database with roles and default user
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    try
    {
        await IdentityDataSetup.SeedData(userManager, roleManager, configuration);
    }
    catch (Exception ex)
    {
        // Log the error or handle it as needed
        Log.Error("An error occurred while seeding the database: {ErrorMessage}", ex.Message);
    }
}


app.Run();