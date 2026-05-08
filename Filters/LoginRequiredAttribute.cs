
// FILE: Filters/LoginRequiredAttribute.cs
// MỤC ĐÍCH: Filter kiểm tra đăng nhập trước khi vào action.
//           Gắn [LoginRequired] lên Controller hoặc Action
//           để chặn truy cập nếu chưa đăng nhập.
//
// CÁCH HOẠT ĐỘNG:
//   - Kế thừa ActionFilterAttribute → tự động chạy trước action.
//   - Kiểm tra Session["UserId"]: null → chưa đăng nhập.
//   - Nếu chưa đăng nhập → redirect về /Account/Login?returnUrl=...
//     returnUrl là đường dẫn hiện tại → sau khi login xong,
//     hệ thống tự redirect về đúng trang khách muốn vào.
//
// VÍ DỤ SỬ DỤNG:
//   [LoginRequired]               // Áp dụng toàn bộ Controller
//   public class CartController : Controller { ... }
//
//   [LoginRequired]               // Áp dụng một action cụ thể
//   public IActionResult Profile() { ... }
// ============================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MilkStore.Filters;

/// <summary>
/// Action filter kiểm tra người dùng đã đăng nhập chưa.
/// Nếu chưa → redirect về trang Login, kèm returnUrl để quay lại sau.
/// </summary>
public class LoginRequiredAttribute : ActionFilterAttribute
{
    // --------------------------------------------------------
    // OnActionExecuting: chạy TRƯỚC khi action method thực thi.
    // Nếu set context.Result ở đây → action gốc sẽ KHÔNG chạy,
    // request bị chặn và trả về kết quả redirect ngay lập tức.
    // --------------------------------------------------------
    /// <summary>
    /// Kiểm tra Session trước mỗi request.
    /// Nếu không có UserId trong session → redirect về trang Login.
    /// </summary>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        var userId  = session.GetInt32("UserId");

        if (userId == null)
        {
            // Lưu lại URL hiện tại vào returnUrl để sau khi login
            // hệ thống có thể redirect người dùng về đúng trang họ muốn vào.
            // Ví dụ: /Cart/Index → sau login → quay về /Cart/Index
            context.Result = new RedirectToActionResult("Login", "Account", new
            {
                returnUrl = context.HttpContext.Request.Path
            });
        }

        base.OnActionExecuting(context);
    }
}
