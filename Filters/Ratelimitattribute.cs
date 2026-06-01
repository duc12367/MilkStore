// FILE: Filters/RateLimitAttribute.cs
// MỤC ĐÍCH: Giới hạn số lần gọi một action trong khoảng thời gian nhất định
//           dựa trên IP address — không cần thư viện ngoài.
//
// CÁCH DÙNG:
//   [RateLimit(maxRequests: 5, windowSeconds: 60)]
//   public async Task<IActionResult> Login(...) { ... }
//
// CƠ CHẾ:
//   - Dùng IMemoryCache lưu counter theo key = "rl:{action}:{ip}"
//   - Mỗi request tăng counter; nếu vượt maxRequests → trả 429
//   - Counter tự hết hạn sau windowSeconds
//   - Thread-safe nhờ lock per key (đủ dùng cho scale đơn server)

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;

namespace MilkStore.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RateLimitAttribute(int maxRequests = 10, int windowSeconds = 60) : ActionFilterAttribute
{
    // Lưu lock object riêng per key để tránh tranh chấp toàn cục
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, object>
        _locks = new();

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var cache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();

        // Lấy IP thực (hỗ trợ reverse proxy như Render/Nginx)
        var ip = context.HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                 ?? context.HttpContext.Connection.RemoteIpAddress?.ToString()
                 ?? "unknown";

        // Lấy tên action để mỗi endpoint có counter riêng
        var action = context.ActionDescriptor.DisplayName ?? "unknown";
        var key = $"rl:{action}:{ip}";
        var lockObj = _locks.GetOrAdd(key, _ => new object());

        lock (lockObj)
        {
            int count = cache.GetOrCreate(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(windowSeconds);
                return 0;
            });

            count++;
            cache.Set(key, count, TimeSpan.FromSeconds(windowSeconds));

            if (count > maxRequests)
            {
                // Thêm header Retry-After để client biết chờ bao lâu
                context.HttpContext.Response.Headers["Retry-After"] = windowSeconds.ToString();

                // Nếu request AJAX/API → trả JSON
                if (context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                    context.HttpContext.Request.Headers["Accept"].ToString().Contains("application/json"))
                {
                    context.Result = new JsonResult(new
                    {
                        error = $"Quá nhiều yêu cầu. Vui lòng thử lại sau {windowSeconds} giây."
                    })
                    { StatusCode = 429 };
                }
                else
                {
                    // Trang thường → redirect về form kèm thông báo lỗi
                    var tempData = context.HttpContext.RequestServices
                        .GetRequiredService<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory>()
                        .GetTempData(context.HttpContext);
                    tempData["Error"] = $"Quá nhiều lần thử. Vui lòng chờ {windowSeconds} giây rồi thử lại.";

                    // Redirect về trang trước (referer) hoặc về /
                    var referer = context.HttpContext.Request.Headers["Referer"].ToString();
                    context.Result = string.IsNullOrEmpty(referer)
                        ? new RedirectResult("/")
                        : new RedirectResult(referer);
                }
            }
        }

        base.OnActionExecuting(context);
    }
}