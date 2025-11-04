using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Components;
using BingoBoard.Admin.Components;
using BingoBoard.Admin.Endpoints;
using BingoBoard.Admin.Hubs;
using BingoBoard.Admin.Services;
using Scalar.AspNetCore;

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

// Configure OpenAPI support
builder.Services.AddOpenApi();
// Add validation support
builder.Services.AddValidation();

builder.AddApplicationDbContext();

builder.Services.AddDefaultIdentity()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
});

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

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(frontendURL.Split(','))
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Register custom services
builder.Services.AddScoped<IBingoService, BingoService>();
builder.Services.AddScoped<IClientConnectionService, ClientConnectionService>();
builder.Services.AddScoped<IBingoSquareService, BingoSquareService>();

// Add HttpClient for API calls within the app
builder.Services.AddScoped(sp => 
{
    var httpClient = new HttpClient
    {
        BaseAddress = new Uri(sp.GetRequiredService<NavigationManager>().BaseUri)
    };
    return httpClient;
});

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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseStaticFiles();
app.MapStaticAssets();

// Use CORS  
app.UseCors();

app.UseAntiforgery();

// Map Razor components
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map SignalR hub without authentication (anonymous access allowed)
app.MapHub<BingoHub>("/bingohub");

// Map authentication endpoints
app.MapAuthenticationEndpoints();

// Map bingo square CRUD endpoints
app.MapBingoSquareCrudEndpoints();

app.Run();
