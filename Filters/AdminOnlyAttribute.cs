// FILE: Filters/AdminOnlyAttribute.cs
// [FIX KM18] Redirect về Login (kèm thông báo) thay vì âm thầm về Home
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MilkStore.Filters;

public class AdminOnlyAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        var roleId = session.GetInt32("RoleId");

        // Chưa đăng nhập → về trang Login
        if (roleId == null)
        {
            context.Result = new RedirectToActionResult("Login", "Account",
                new { area = "", returnUrl = context.HttpContext.Request.Path });
            return;
        }

        // Đã đăng nhập nhưng không phải admin → báo lỗi rõ ràng
        if (roleId != 1)
        {
            context.HttpContext.Session.SetString("FlashError",
                "Bạn không có quyền truy cập trang quản trị.");
            context.Result = new RedirectToActionResult("Index", "Home", new { area = "" });
        }

        base.OnActionExecuting(context);
    }
}