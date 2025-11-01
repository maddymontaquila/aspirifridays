using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using BingoBoard.Data;

namespace BingoBoard.Admin.Endpoints;

/// <summary>
/// Extension methods for mapping authentication-related endpoints
/// </summary>
public static class AuthenticationEndpoints
{
    /// <summary>
    /// Maps all authentication-related endpoints to the web application
    /// </summary>
    /// <param name="app">The web application instance</param>
    /// <returns>The web application instance for method chaining</returns>
    public static WebApplication MapAuthenticationEndpoints(this WebApplication app)
    {
        app.MapPost("/auth/login", LoginHandler);
        app.MapPost("/auth/logout", LogoutHandler);
        app.MapPost("/passkey/creation-options", PasskeyCreationOptionsHandler);
        app.MapPost("/passkey/request-options", PasskeyRequestOptionsHandler);
        
        return app;
    }

    /// <summary>
    /// Handles user authentication via password or passkey credentials
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="userManager">The user manager service</param>
    /// <param name="signInManager">The sign-in manager service</param>
    /// <param name="logger">The logger instance</param>
    /// <returns>A redirect result based on authentication outcome</returns>
    internal static async Task<RedirectHttpResult> LoginHandler(
        HttpContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<Program> logger)
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
            return TypedResults.Redirect("/login?error=no-user");
        }

        // Handle passkey errors
        if (!string.IsNullOrEmpty(passkeyError))
        {
            logger.LogWarning("Passkey error from client: {Error}", passkeyError);
            return TypedResults.Redirect("/login?error=passkey-error");
        }

        // Try passkey authentication first if passkey credential is provided
        if (!string.IsNullOrEmpty(passkeyCredentialJson))
        {
            logger.LogInformation("Attempting passkey authentication");

            var result = await signInManager.PasskeySignInAsync(passkeyCredentialJson);
            if (result.Succeeded)
            {
                logger.LogInformation("Passkey authenticated succeeded, signing in user");
                return TypedResults.Redirect("/");
            }

            logger.LogWarning("Passkey authentication failed");
            return TypedResults.Redirect("/login?error=passkey-error");
        }

        // Try password authentication if password is provided
        if (!string.IsNullOrEmpty(password))
        {
            logger.LogInformation("Attempting password authentication");

            var result = await signInManager.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                logger.LogInformation("Password valid, signing in user");
                return TypedResults.Redirect("/");
            }

            logger.LogInformation("Password invalid, redirecting to login with error");
            return TypedResults.Redirect("/login?error=invalid");
        }

        // No authentication method provided
        logger.LogInformation("No authentication credentials provided");
        return TypedResults.Redirect("/login?error=invalid");
    }

    /// <summary>
    /// Handles user logout by signing out the current user
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="signInManager">The sign-in manager service</param>
    /// <returns>A redirect result to the login page</returns>
    internal static async Task<RedirectHttpResult> LogoutHandler(
        HttpContext context,
        SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return TypedResults.Redirect("/login");
    }

    /// <summary>
    /// Generates passkey creation options for the authenticated user
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="userManager">The user manager service</param>
    /// <param name="signInManager">The sign-in manager service</param>
    /// <param name="antiforgery">The antiforgery service</param>
    /// <returns>JSON content containing passkey creation options</returns>
    internal static async Task<Results<NotFound<string>, ContentHttpResult>> PasskeyCreationOptionsHandler(
        HttpContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IAntiforgery antiforgery)
    {
        await antiforgery.ValidateRequestAsync(context);

        var user = await userManager.GetUserAsync(context.User);
        if (user is null)
        {
            return TypedResults.NotFound($"Unable to load user with ID '{userManager.GetUserId(context.User)}'.");
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
    }

    /// <summary>
    /// Generates passkey request options for authentication
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="userManager">The user manager service</param>
    /// <param name="signInManager">The sign-in manager service</param>
    /// <param name="antiforgery">The antiforgery service</param>
    /// <param name="username">Optional username for user-specific passkey options</param>
    /// <returns>JSON content containing passkey request options</returns>
    internal static async Task<ContentHttpResult> PasskeyRequestOptionsHandler(
        HttpContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IAntiforgery antiforgery,
        string? username)
    {
        await antiforgery.ValidateRequestAsync(context);

        var user = string.IsNullOrEmpty(username) ? null : await userManager.FindByNameAsync(username);
        var optionsJson = await signInManager.MakePasskeyRequestOptionsAsync(user);
        return TypedResults.Content(optionsJson, contentType: "application/json");
    }
}