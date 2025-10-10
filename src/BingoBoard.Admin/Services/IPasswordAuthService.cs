namespace BingoBoard.Admin.Services;

public interface IPasswordAuthService
{
    bool ValidatePassword(string password);
    Task<bool> IsAuthenticatedAsync(HttpContext context);
    Task SignInAsync(HttpContext context);
    Task SignOutAsync(HttpContext context);
}
