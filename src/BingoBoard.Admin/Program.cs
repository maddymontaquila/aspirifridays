using BingoBoard.Admin.Components;
using BingoBoard.Admin.Hubs;
using BingoBoard.Admin.Services;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

// Check if authentication should be disabled for development
var disableAuth = builder.Configuration.GetValue<bool>("Development:DisableAuthentication");

if (!disableAuth)
{
    // Add Microsoft Identity authentication (only when not disabled)
    builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(options =>
        {
            options.Instance = "https://login.microsoftonline.com/";
            options.TenantId = builder.Configuration["Authentication:Microsoft:TenantId"];
            options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"];
            options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"];
            options.CallbackPath = "/signin-oidc";
            options.ResponseType = "code";
            options.SaveTokens = true;
        });

    // Add authorization
    builder.Services.AddAuthorization(options =>
    {
        // Default policy requires authentication for all pages except SignalR hub
        options.DefaultPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
        
        // Policy for admin access - can be extended with specific claims/roles
        options.AddPolicy("AdminOnly", policy =>
            policy.RequireAuthenticatedUser());
    });
}
else
{
    // Development mode: Add minimal authorization that allows anonymous access
    builder.Services.AddAuthorization(options =>
    {
        options.DefaultPolicy = new AuthorizationPolicyBuilder()
            .RequireAssertion(_ => true) // Always allow
            .Build();
        
        options.AddPolicy("AdminOnly", policy =>
            policy.RequireAssertion(_ => true)); // Always allow in development
    });
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add SignalR
builder.Services.AddSignalR();

// Add CORS for development (allows frontend to connect to SignalR)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("http+https://bingoboard")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Required for SignalR
    });
});

// Add Redis with Aspire client for distributed caching and raw Redis access
builder.AddRedisDistributedCache(connectionName: "cache");

// Register custom services
builder.Services.AddScoped<IBingoService, BingoService>();
builder.Services.AddScoped<IClientConnectionService, ClientConnectionService>();

// Register background services
builder.Services.AddHostedService<ApprovalCleanupService>();

// Add logging
builder.Services.AddLogging();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.MapStaticAssets();

// Use CORS  
app.UseCors();

// Add authentication and authorization middleware (only if authentication is enabled)
if (!disableAuth)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.UseAntiforgery();

// Map Razor components with conditional authentication
if (disableAuth)
{
    // Development mode: Allow anonymous access
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode()
        .AllowAnonymous();
}
else
{
    // Production mode: Require authentication
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode()
        .RequireAuthorization("AdminOnly");
}

// Map SignalR hub without authentication (anonymous access allowed)
app.MapHub<BingoHub>("/bingohub").AllowAnonymous();

// Add login and logout endpoints (only if authentication is enabled)
if (!disableAuth)
{
    app.MapGet("/login", async (HttpContext context) =>
    {
        await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme);
    }).AllowAnonymous();

    app.MapPost("/logout", async (HttpContext context) =>
    {
        await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
        await context.SignOutAsync("Cookies");
        return Results.Redirect("/");
    }).RequireAuthorization();
}

app.Run();
