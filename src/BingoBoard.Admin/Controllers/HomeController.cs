using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BingoBoard.Admin.Controllers;

[Authorize]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return Content($"Welcome! You are authenticated as: {User.Identity?.Name}");
    }
    
    [AllowAnonymous]
    public IActionResult Test()
    {
        return Content($"Test page - Authenticated: {User.Identity?.IsAuthenticated}, Name: {User.Identity?.Name}");
    }
}
