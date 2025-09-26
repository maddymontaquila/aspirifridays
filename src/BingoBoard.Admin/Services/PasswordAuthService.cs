using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace BingoBoard.Admin.Services;

public class PasswordAuthService : IPasswordAuthService
{
    private readonly string _password;

    public PasswordAuthService(IConfiguration configuration)
    {
        if (string.IsNullOrEmpty(configuration["Authentication:AdminPassword"]))
        {
            throw new ArgumentException("Admin password is not configured. Please set the 'Authentication:AdminPassword' environment variable.");
        }
        _password = configuration["Authentication:AdminPassword"]!;
    }

    public bool ValidatePassword(string password)
    {
        return _password.Equals(password, StringComparison.Ordinal);
    }

    public async Task<bool> IsAuthenticatedAsync(HttpContext context)
    {
        var result = await context.AuthenticateAsync("Cookies");
        var isAuthenticated = result.Succeeded;
        
        // Add logging to debug authentication status
        var logger = context.RequestServices.GetService<ILogger<PasswordAuthService>>();
        logger?.LogInformation("Authentication check - Succeeded: {IsAuthenticated}, Principal: {Principal}", 
            isAuthenticated, result.Principal?.Identity?.Name ?? "null");
        
        return isAuthenticated;
    }

    public async Task SignInAsync(HttpContext context)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "Admin"),
            new Claim(ClaimTypes.Role, "Administrator")
        };

        var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        await context.SignInAsync("Cookies", claimsPrincipal);
    }

    public async Task SignOutAsync(HttpContext context)
    {
        await context.SignOutAsync("Cookies");
    }
}
