using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkStore.Filters;
using MilkStore.Models;

namespace MilkStore.Areas.Admin.Controllers;

[Area("Admin")]
[AdminOnly]
public class DashboardController(MilkStore4Context db) : Controller
{
    public async Task<IActionResult> Index()
    {
        // [FIX TC16] Bọc toàn bộ trong try-catch để không crash khi mất kết nối DB
        try
        {
            var now = DateTime.UtcNow;
            var thisMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var lastMonthStart = thisMonthStart.AddMonths(-1);

            // ── Stat cards ────────────────────────────────────────────
            ViewBag.TotalProducts = await db.Products.CountAsync();

            var totalOrdersThisMonth = await db.Orders
                .CountAsync(o => o.OrderDate >= thisMonthStart);
            var totalOrdersLastMonth = await db.Orders
                .CountAsync(o => o.OrderDate >= lastMonthStart && o.OrderDate < thisMonthStart);

            ViewBag.TotalOrders = totalOrdersThisMonth;

            // [FIX TC02, bổ sung] % thay đổi đơn hàng so tháng trước — thực tế, không hardcode
            ViewBag.OrderChange = totalOrdersLastMonth == 0 ? 100 :
                Math.Round((double)(totalOrdersThisMonth - totalOrdersLastMonth)
                           / totalOrdersLastMonth * 100, 1);

            var revenueThisMonth = await db.Orders
                .Where(o => o.OrderDate >= thisMonthStart)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
            var revenueLastMonth = await db.Orders
                .Where(o => o.OrderDate >= lastMonthStart && o.OrderDate < thisMonthStart)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            ViewBag.TotalRevenue = revenueThisMonth;

            // [FIX TC02] % doanh thu so tháng trước — thực tế
            ViewBag.RevenueChange = revenueLastMonth == 0 ? 100 :
                Math.Round((double)((revenueThisMonth - revenueLastMonth) / revenueLastMonth * 100), 1);

            // User model không có CreatedAt → chỉ hiển thị tổng, không tính % thay đổi
            ViewBag.TotalUsers = await db.Users.CountAsync(u => u.RoleId == 2);
            ViewBag.UserChange = 0.0;

            ViewBag.ProductChange = 0; // sản phẩm không cần % thay đổi

            // ── Biểu đồ doanh thu 7 ngày ─────────────────────────────
            var cutoff7 = now.Date.AddDays(-6);
            var rawOrders = await db.Orders
                .Where(o => o.OrderDate >= cutoff7)
                .Select(o => new { o.OrderDate, o.TotalAmount })
                .ToListAsync();

            var last7 = Enumerable.Range(0, 7)
                .Select(i => now.Date.AddDays(-6 + i)).ToList();
            var revenueRaw = rawOrders
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(o => o.TotalAmount) })
                .ToList();

            ViewBag.RevenueLabels = last7.Select(d => d.ToString("dd/MM")).ToArray();
            ViewBag.RevenueData = last7
                .Select(d => revenueRaw.FirstOrDefault(r => r.Date == d)?.Total ?? 0)
                .ToArray();

            // ── Biểu đồ doanh thu 12 tháng (TC18) ───────────────────
            var cutoff12 = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-11);
            var rawMonthly = await db.Orders
                .Where(o => o.OrderDate >= cutoff12)
                .Select(o => new { o.OrderDate, o.TotalAmount })
                .ToListAsync();

            var last12 = Enumerable.Range(0, 12)
                .Select(i => cutoff12.AddMonths(i)).ToList();
            var monthlyRaw = rawMonthly
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(o => o.TotalAmount) })
                .ToList();

            ViewBag.MonthLabels = last12.Select(d => d.ToString("MM/yyyy")).ToArray();
            ViewBag.MonthData = last12
                .Select(d => monthlyRaw
                    .FirstOrDefault(r => r.Year == d.Year && r.Month == d.Month)?.Total ?? 0)
                .ToArray();

            // ── Sản phẩm bán chạy ────────────────────────────────────
            var topProductIds = await db.OrderItems
                .GroupBy(oi => oi.ProductId)
                .Select(g => new { ProductId = g.Key, Sold = g.Sum(x => x.Quantity) })
                .OrderByDescending(x => x.Sold)
                .Take(5)
                .ToListAsync();

            var productNames = await db.Products
                .Where(p => topProductIds.Select(x => x.ProductId).Contains(p.Id))
                .Select(p => new { p.Id, p.ProductName })
                .ToListAsync();

            var topProducts = topProductIds
                .Join(productNames, x => x.ProductId, p => p.Id,
                    (x, p) => new { p.ProductName, x.Sold })
                .ToList();

            ViewBag.TopLabels = topProducts.Select(x => x.ProductName).ToArray();
            ViewBag.TopData = topProducts.Select(x => x.Sold).ToArray();

            // ── Đơn hàng gần đây ─────────────────────────────────────
            ViewBag.RecentOrders = await db.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(8).ToListAsync();

            // [FIX TC07] Cờ báo DB rỗng
            ViewBag.IsEmpty = ViewBag.TotalOrders == 0 && ViewBag.TotalProducts == 0;
            ViewBag.DbError = false;
        }
        catch (Exception)
        {
            // [FIX TC16] Mất kết nối DB → hiển thị trang lỗi thay vì crash 500
            ViewBag.DbError = true;
            ViewBag.DbErrMsg = "Không thể kết nối cơ sở dữ liệu. Vui lòng thử lại sau.";
            // Gán giá trị mặc định để view không null-ref
            ViewBag.TotalRevenue = 0m;
            ViewBag.TotalOrders = 0;
            ViewBag.TotalUsers = 0;
            ViewBag.TotalProducts = 0;
            ViewBag.RevenueChange = 0.0;
            ViewBag.OrderChange = 0.0;
            ViewBag.UserChange = 0.0;
            ViewBag.RevenueLabels = Array.Empty<string>();
            ViewBag.RevenueData = Array.Empty<decimal>();
            ViewBag.MonthLabels = Array.Empty<string>();
            ViewBag.MonthData = Array.Empty<decimal>();
            ViewBag.TopLabels = Array.Empty<string>();
            ViewBag.TopData = Array.Empty<int>();
            ViewBag.RecentOrders = new List<Order>();
            ViewBag.IsEmpty = false;
        }

        return View();
    }

    public IActionResult Chat()
    {
        return View("~/Areas/Admin/Views/Dashboard/Chat.cshtml");
    }
}