// FILE: Filters/BlockedUserFilter.cs
// MỤC ĐÍCH: Middleware-level filter kiểm tra user có bị block không
//           sau mỗi request — không chờ hết session 2 giờ.
//
// CÁCH HOẠT ĐỘNG:
//   - Đăng ký làm global filter trong Program.cs
//   - Mỗi request: nếu có UserId trong session → query DB kiểm tra IsBlocked
//   - Nếu bị block → xóa session, redirect về Login
//   - Dùng IMemoryCache cache 30 giây/user để tránh query DB liên tục

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MilkStore.Models;

namespace MilkStore.Filters;

public class BlockedUserFilter(MilkStore4Context db, IMemoryCache cache) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var session = context.HttpContext.Session;
        var userId = session.GetInt32("UserId");

        if (userId.HasValue)
        {
            var cacheKey = $"user_blocked:{userId}";

            // Cache 30 giây để không query DB mỗi request
            bool isBlocked = await cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
                var user = await db.Users.AsNoTracking()
                    .Where(u => u.Id == userId.Value)
                    .Select(u => u.IsBlocked)
                    .FirstOrDefaultAsync();
                return user;
            });

            if (isBlocked)
            {
                session.Clear();
                context.Result = new RedirectToActionResult("Login", "Account", new
                {
                    area = "",
                    message = "Tài khoản của bạn đã bị khóa."
                });
                return;
            }
        }

        await next();
    }
}