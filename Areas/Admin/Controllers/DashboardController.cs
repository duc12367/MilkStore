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
        ViewBag.TotalProducts = await db.Products.CountAsync();
        ViewBag.TotalOrders = await db.Orders.CountAsync();
        ViewBag.TotalUsers = await db.Users.CountAsync(u => u.RoleId == 2);
        ViewBag.TotalRevenue = await db.Orders
            .SumAsync(o => o.TotalAmount);

        var last7 = Enumerable.Range(0, 7)
            .Select(i => DateTime.UtcNow.Date.AddDays(-6 + i)).ToList();

        // FIX: GroupBy(o => o.OrderDate.Date) không dịch được sang SQL trên PostgreSQL/Npgsql
        // Giải pháp: lấy dữ liệu về bộ nhớ rồi group ở phía C#
        // (đã lọc Where trước nên lượng dữ liệu nhỏ, không ảnh hưởng hiệu năng)
        var cutoff = DateTime.UtcNow.Date.AddDays(-6);
        var rawOrders = await db.Orders
            .Where(o => o.OrderDate >= cutoff)
            .Select(o => new { o.OrderDate, o.TotalAmount })
            .ToListAsync();

        var revenueRaw = rawOrders
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(o => o.TotalAmount) })
            .ToList();

        ViewBag.RevenueLabels = last7.Select(d => d.ToString("dd/MM")).ToArray();
        ViewBag.RevenueData = last7
            .Select(d => revenueRaw.FirstOrDefault(r => r.Date == d)?.Total ?? 0)
            .ToArray();

        // FIX: GroupBy + Join không luôn translate được sang SQL trong EF Core với PostgreSQL
        // Tách thành 2 query: GroupBy riêng, rồi lấy tên sản phẩm sau
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

        ViewBag.RecentOrders = await db.Orders
            .Include(o => o.User)
            .OrderByDescending(o => o.OrderDate)
            .Take(8).ToListAsync();

        return View();
    }

    public IActionResult Chat()
    {
        return View("~/Areas/Admin/Views/Dashboard/Chat.cshtml");
    }
}