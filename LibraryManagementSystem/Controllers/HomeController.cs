using Microsoft.AspNetCore.Mvc;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (!User.Identity.IsAuthenticated)
        {
            return RedirectToPage("/Identity/Account/Login");
        }

        // Redirect based on role
        if (User.IsInRole("Bibliotecário"))
        {
            return RedirectToAction("Index", "Admin");
        }

        return View(); // Member dashboard
    }
}
