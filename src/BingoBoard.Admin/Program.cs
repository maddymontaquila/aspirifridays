using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using BingoBoard.Admin.Components;
using BingoBoard.Admin.Hubs;
using BingoBoard.Admin.Services;
using BingoBoard.Data;

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
    var passkeyCredentialJson = form["passkey.CredentialJson"].ToString();
    var passkeyError = form["passkey.Error"].ToString();

    logger.LogInformation("Login attempt - Has password: {HasPassword}, Has passkey: {HasPasskey}, Passkey error: {PasskeyError}",
        !string.IsNullOrEmpty(password),
        !string.IsNullOrEmpty(passkeyCredentialJson),
        passkeyError);

    var user = await userManager.FindByNameAsync("admin");
    if (user is null)
    {
        logger.LogInformation("Admin user not created.");
        return Results.Redirect("/login?error=no-user");
    }

    // Handle passkey errors
    if (!string.IsNullOrEmpty(passkeyError))
    {
        logger.LogWarning("Passkey error from client: {Error}", passkeyError);
        return Results.Redirect("/login?error=passkey-error");
    }

    // Try passkey authentication first if passkey credential is provided
    if (!string.IsNullOrEmpty(passkeyCredentialJson))
    {
        logger.LogInformation("Attempting passkey authentication");

        var result = await signInManager.PasskeySignInAsync(passkeyCredentialJson);
        if (result.Succeeded)
        {
            logger.LogInformation("Passkey authenticated succeeded, signing in user");
            return Results.Redirect("/");
        }

        logger.LogWarning("Passkey authentication failed");
        return Results.Redirect("/login?error=passkey-error");
    }

    // Try password authentication if password is provided
    if (!string.IsNullOrEmpty(password))
    {
        logger.LogInformation("Attempting password authentication");

        var result = await signInManager.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            logger.LogInformation("Password valid, signing in user");
            return Results.Redirect("/");
        }

        logger.LogInformation("Password invalid, redirecting to login with error");
        return Results.Redirect("/login?error=invalid");
    }

    // No authentication method provided
    logger.LogInformation("No authentication credentials provided");
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
