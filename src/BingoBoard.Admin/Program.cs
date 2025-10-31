using BingoBoard.Admin.Components;
using BingoBoard.Admin.Hubs;
using BingoBoard.Admin.Services;
using Microsoft.AspNetCore.Identity;
using BingoBoard.Data;
using Microsoft.AspNetCore.Antiforgery;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

// Get the frontend URL from service discovery
var frontendURL = Environment.GetEnvironmentVariable("services__bingoboard__http__0") ??
                  Environment.GetEnvironmentVariable("services__bingoboard__https__0") ??
                  "http+https://bingoboard"; // Fallback to hardcoded value if service discovery not available

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();
builder.Services.AddAuthorization();

builder.AddApplicationDbContext();

builder.Services.AddDefaultIdentity()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
});

// Add controllers for testing
builder.Services.AddControllers();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<RedirectManager>();

// Add SignalR
builder.Services.AddSignalR()
    .AddStackExchangeRedis(builder.Configuration.GetConnectionString("cache")!);

// Add Redis with Aspire client for distributed caching and raw Redis access
builder.AddRedisDistributedCache(connectionName: "cache");

// Register custom services
builder.Services.AddScoped<IBingoService, BingoService>();
builder.Services.AddScoped<IClientConnectionService, ClientConnectionService>();

// Register background services
builder.Services.AddHostedService<ApprovalCleanupService>();

builder.Services.AddSingleton<AddressResolver>();

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

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseStaticFiles();
app.MapStaticAssets();

// Use CORS  
app.UseCors();

app.UseAntiforgery();

// Map controllers for testing
app.MapControllers();

// Map Razor components
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map SignalR hub without authentication (anonymous access allowed)
app.MapHub<BingoHub>("/bingohub");

// Add login and logout endpoints
app.MapPost("/auth/login", async (
    HttpContext context,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ILogger<Program> logger) =>
{
    // Read password from form data instead of query parameter
    var form = await context.Request.ReadFormAsync();
    var password = form["password"].ToString();

    logger.LogInformation("Login attempt with password: {Password}", password);

    var user = await userManager.FindByNameAsync("admin");
    if (user is null)
    {
        logger.LogInformation("Admin user not created.");
        return Results.Redirect("/login?error=nouser");
    }

    var result = await signInManager.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: false);
    if (result.Succeeded)
    {
        logger.LogInformation("Password valid, signing in user");
        return Results.Redirect("/"); // Redirect to simple test endpoint
    }

    logger.LogInformation("Password invalid, redirecting to login with error");
    return Results.Redirect("/login?error=invalid");
});

app.MapPost("/auth/logout", async (HttpContext context, SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/login");
});

app.MapPost("/passkey/creation-options", async (
    HttpContext context,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IAntiforgery antiforgery) =>
{
    await antiforgery.ValidateRequestAsync(context);

    var user = await userManager.GetUserAsync(context.User);
    if (user is null)
    {
        return Results.NotFound($"Unable to load user with ID '{userManager.GetUserId(context.User)}'.");
    }

    var userId = await userManager.GetUserIdAsync(user);
    var userName = await userManager.GetUserNameAsync(user) ?? "User";
    var optionsJson = await signInManager.MakePasskeyCreationOptionsAsync(new()
    {
        Id = userId,
        Name = userName,
        DisplayName = userName
    });
    return TypedResults.Content(optionsJson, contentType: "application/json");
});

app.MapPost("/passkey/request-options", async (
    HttpContext context,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IAntiforgery antiforgery,
    string? username) =>
{
    await antiforgery.ValidateRequestAsync(context);

    var user = string.IsNullOrEmpty(username) ? null : await userManager.FindByNameAsync(username);
    var optionsJson = await signInManager.MakePasskeyRequestOptionsAsync(user);
    return TypedResults.Content(optionsJson, contentType: "application/json");
});

app.Run();
