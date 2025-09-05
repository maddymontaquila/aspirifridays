using BingoBoard.Admin.Components;
using BingoBoard.Admin.Hubs;
using BingoBoard.Admin.Services;
using BingoBoard.Admin.Middleware;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

// Get the frontend URL from service discovery
var frontendURL = Environment.GetEnvironmentVariable("services__bingoboard__http__0") ?? 
                  Environment.GetEnvironmentVariable("services__bingoboard__https__0") ?? 
                  "http+https://bingoboard"; // Fallback to hardcoded value if service discovery not available

// Add authentication services (required even for custom auth)
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;
        options.Cookie.Name = "BingoAdmin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization();

// Add controllers for testing
builder.Services.AddControllers();

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
            .WithOrigins(frontendURL)
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
builder.Services.AddScoped<IPasswordAuthService, PasswordAuthService>();

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

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Add password authentication middleware (temporarily disabled for debugging)
// app.UseMiddleware<PasswordAuthMiddleware>();

app.UseAntiforgery();

// Map controllers for testing
app.MapControllers();

// Map Razor components
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map SignalR hub without authentication (anonymous access allowed)
app.MapHub<BingoHub>("/bingohub");

// Add login and logout endpoints
app.MapPost("/auth/login", async (HttpContext context, IPasswordAuthService authService, ILogger<Program> logger) =>
{
    // Read password from form data instead of query parameter
    var form = await context.Request.ReadFormAsync();
    var password = form["password"].ToString();
    
    logger.LogInformation("Login attempt with password: {Password}", password);
    
    if (authService.ValidatePassword(password))
    {
        logger.LogInformation("Password valid, signing in user");
        await authService.SignInAsync(context);
        
        // Check if user was actually signed in
        var authResult = await context.AuthenticateAsync("Cookies");
        logger.LogInformation("After sign in - Authentication succeeded: {Success}", authResult.Succeeded);
        
        return Results.Redirect("/"); // Redirect to simple test endpoint
    }
    
    logger.LogInformation("Password invalid, redirecting to login with error");
    return Results.Redirect("/login?error=invalid");
});

app.MapPost("/auth/logout", async (HttpContext context, IPasswordAuthService authService) =>
{
    await authService.SignOutAsync(context);
    return Results.Redirect("/login");
});

app.Run();
