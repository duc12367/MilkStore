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
            .Where(o => o.Status == "Paid")
            .SumAsync(o => o.TotalAmount);

        var last7 = Enumerable.Range(0, 7)
            .Select(i => DateTime.Today.AddDays(-6 + i)).ToList();

        var revenueRaw = await db.Orders
            .Where(o => o.Status == "Paid" &&
                        o.OrderDate >= DateTime.Today.AddDays(-6))
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(o => o.TotalAmount) })
            .ToListAsync();

        ViewBag.RevenueLabels = last7.Select(d => d.ToString("dd/MM")).ToArray();
        ViewBag.RevenueData = last7
            .Select(d => revenueRaw.FirstOrDefault(r => r.Date == d)?.Total ?? 0)
            .ToArray();

        var topProducts = await db.OrderItems
            .GroupBy(oi => oi.ProductId)
            .Select(g => new { ProductId = g.Key, Sold = g.Sum(x => x.Quantity) })
            .OrderByDescending(x => x.Sold)
            .Take(5)
            .Join(db.Products, x => x.ProductId, p => p.Id,
                (x, p) => new { p.ProductName, x.Sold })
            .ToListAsync();

        ViewBag.TopLabels = topProducts.Select(x => x.ProductName).ToArray();
        ViewBag.TopData = topProducts.Select(x => x.Sold).ToArray();

        ViewBag.RecentOrders = await db.Orders
            .Include(o => o.User)
            .OrderByDescending(o => o.OrderDate)
            .Take(8).ToListAsync();

        return View();
    }
}
