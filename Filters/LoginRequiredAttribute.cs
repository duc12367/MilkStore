using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MilkStore.Filters;

public class LoginRequiredAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        var userId = session.GetInt32("UserId");

        if (userId == null)
        {
            context.Result = new RedirectToActionResult("Login", "Account", new
            {
                returnUrl = context.HttpContext.Request.Path
            });
        }

        base.OnActionExecuting(context);
    }
}
