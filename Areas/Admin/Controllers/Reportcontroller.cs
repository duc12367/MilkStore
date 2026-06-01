using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkStore.Filters;
using MilkStore.Models;
using System.Text;

namespace MilkStore.Areas.Admin.Controllers;

[Area("Admin")]
[AdminOnly]
public class ReportController(MilkStore4Context db) : Controller
{
    public IActionResult Index() => View();

    // Export đơn hàng
    public async Task<IActionResult> ExportOrders(DateTime? from, DateTime? to, string? status)
    {
        try
        {
            var fromUtc = from?.ToUniversalTime() ?? DateTime.UtcNow.AddMonths(-1);
            var toUtc = (to?.ToUniversalTime() ?? DateTime.UtcNow).Date.AddDays(1).AddSeconds(-1);

            var query = db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Where(o => o.OrderDate >= fromUtc && o.OrderDate <= toUtc);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(o => o.Status == status);

            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();

            var sb = new StringBuilder();
            sb.Append('\uFEFF');
            sb.AppendLine("Ma don,Ngay dat,Khach hang,Email,SDT,Dia chi,San pham,Tong tien,Giam gia,Ma giam gia,PTTT,Trang thai");

            foreach (var o in orders)
            {
                var products = string.Join(" | ", o.OrderItems.Select(i =>
                    $"{i.Product?.ProductName} x{i.Quantity}"));

                sb.AppendLine(string.Join(",",
                    $"#{o.Id:D6}",
                    o.OrderDate.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                    Esc(o.User?.FullName ?? ""),
                    Esc(o.User?.Email ?? ""),
                    o.Phone ?? "",
                    Esc(o.ShippingAddress ?? ""),
                    Esc(products),
                    o.TotalAmount.ToString("N0"),
                    (o.DiscountAmount).ToString("N0"),
                    o.CouponCode ?? "",
                    o.PaymentMethod ?? "",
                    o.Status
                ));
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv; charset=utf-8", $"don-hang_{fromUtc:yyyyMMdd}.csv");
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Lỗi xuất đơn hàng: " + ex.Message;
            return RedirectToAction("Index");
        }
    }

    // Export doanh thu theo ngày
    public async Task<IActionResult> ExportRevenue(DateTime? from, DateTime? to)
    {
        try
        {
            var fromUtc = from?.ToUniversalTime() ?? DateTime.UtcNow.AddMonths(-1);
            var toUtc = (to?.ToUniversalTime() ?? DateTime.UtcNow).Date.AddDays(1).AddSeconds(-1);

            // Load về memory trước, GroupBy sau — tránh lỗi EF dịch sang SQL
            var raw = await db.Orders
                .Where(o => o.OrderDate >= fromUtc && o.OrderDate <= toUtc
                            && (o.Status == "Paid" || o.Status == "Shipping" || o.Status == "Pending"))
                .Select(o => new
                {
                    o.OrderDate,
                    o.TotalAmount,
                    Discount = (decimal)0  // tránh lỗi nếu cột chưa có
                })
                .ToListAsync();

            var grouped = raw
                .GroupBy(o => o.OrderDate.ToLocalTime().Date)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Date = g.Key.ToString("dd/MM/yyyy"),
                    Orders = g.Count(),
                    Revenue = g.Sum(x => x.TotalAmount)
                }).ToList();

            var sb = new StringBuilder();
            sb.Append('\uFEFF');
            sb.AppendLine("Ngay,So don,Doanh thu (d)");
            foreach (var r in grouped)
                sb.AppendLine($"{r.Date},{r.Orders},{r.Revenue:N0}");
            sb.AppendLine($"TONG,{grouped.Sum(r => r.Orders)},{grouped.Sum(r => r.Revenue):N0}");

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv; charset=utf-8",
                $"doanh-thu_{fromUtc:yyyyMMdd}.csv");
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Lỗi xuất doanh thu: " + ex.Message;
            return RedirectToAction("Index");
        }
    }

    // Export sản phẩm bán chạy — load memory rồi group, tránh lỗi EF PostgreSQL
    public async Task<IActionResult> ExportProducts(DateTime? from, DateTime? to)
    {
        try
        {
            var fromUtc = from?.ToUniversalTime() ?? DateTime.UtcNow.AddMonths(-1);
            var toUtc = (to?.ToUniversalTime() ?? DateTime.UtcNow).Date.AddDays(1).AddSeconds(-1);

            var raw = await db.OrderItems
                .Include(oi => oi.Product).ThenInclude(p => p.Brand)
                .Include(oi => oi.Product).ThenInclude(p => p.Category)
                .Include(oi => oi.Order)
                .Where(oi => oi.Order.OrderDate >= fromUtc && oi.Order.OrderDate <= toUtc)
                .ToListAsync();  // load hết về memory

            var grouped = raw
                .GroupBy(oi => oi.ProductId)
                .Select(g => new
                {
                    Name = g.First().Product?.ProductName ?? "",
                    Brand = g.First().Product?.Brand?.Name ?? "",
                    Category = g.First().Product?.Category?.Name ?? "",
                    Sold = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.Quantity * x.PriceAtTime)
                })
                .OrderByDescending(x => x.Sold)
                .ToList();

            var sb = new StringBuilder();
            sb.Append('\uFEFF');
            sb.AppendLine("San pham,Thuong hieu,Danh muc,So luong ban,Doanh thu (d)");
            foreach (var r in grouped)
                sb.AppendLine($"{Esc(r.Name)},{r.Brand},{r.Category},{r.Sold},{r.Revenue:N0}");

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv; charset=utf-8",
                $"san-pham-ban-chay_{fromUtc:yyyyMMdd}.csv");
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Lỗi xuất sản phẩm: " + ex.Message;
            return RedirectToAction("Index");
        }
    }

    private static string Esc(string s)
    {
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
            return $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }
}