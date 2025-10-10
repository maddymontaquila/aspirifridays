using BingoBoard.Admin.Services;

namespace BingoBoard.Admin.Middleware;

public class PasswordAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PasswordAuthMiddleware> _logger;

    public PasswordAuthMiddleware(RequestDelegate next, ILogger<PasswordAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IPasswordAuthService authService)
    {
        // Allow access to login page, logout endpoint, static files, and SignalR hub
        var path = context.Request.Path.Value?.ToLowerInvariant();
        if (path == "/login" || 
            path == "/logout" || 
            path?.StartsWith("/_") == true || 
            path?.StartsWith("/lib") == true ||
            path?.StartsWith("/bingohub") == true ||
            path?.StartsWith("/auth/") == true || // Allow auth endpoints
            path?.Contains("css") == true ||
            path?.Contains("js") == true ||
            path?.Contains("favicon") == true ||
            path?.Contains("_framework") == true)
        {
            await _next(context);
            return;
        }

        // Check if user is authenticated
        var isAuthenticated = await authService.IsAuthenticatedAsync(context);
        
        _logger.LogInformation("Path: {Path}, Authenticated: {IsAuthenticated}", path, isAuthenticated);
        
        if (!isAuthenticated)
        {
            // Redirect to login page
            _logger.LogInformation("User not authenticated, redirecting to login");
            context.Response.Redirect("/login");
            return;
        }

        await _next(context);
    }
}
