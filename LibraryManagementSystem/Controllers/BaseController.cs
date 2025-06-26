using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.Controllers
{
    public class BaseController : Controller
    {
        protected void SetLayoutBasedOnRole()
        {
            if (User.IsInRole("Bibliotecário"))
            {
                ViewData["Layout"] = "_AdminLayout";
            }
            else if (User.IsInRole("Membro"))
            {
                ViewData["Layout"] = "_Layout";
            }
            else
            {
                ViewData["Layout"] = "_Layout"; // Default for unauthenticated users
            }
        }

        public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            SetLayoutBasedOnRole();
            base.OnActionExecuting(context);
        }
    }
}
