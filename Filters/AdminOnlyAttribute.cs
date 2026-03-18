using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MilkStore.Filters;

public class AdminOnlyAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        var roleId  = session.GetInt32("RoleId");

        if (roleId == null)
        {
            context.Result = new RedirectToActionResult("Login", "Account", new { area = "" });
            return;
        }

        if (roleId != 1)
            context.Result = new RedirectToActionResult("Index", "Home", new { area = "" });

        base.OnActionExecuting(context);
    }
}
