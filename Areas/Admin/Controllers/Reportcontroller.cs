// FILE: Areas/Admin/Controllers/ReportController.cs
//  Export báo cáo doanh thu & đơn hàng ra file CSV/Excel.
// Dùng CSV thuần (không cần thư viện) — mở được trong Excel, Google Sheets.

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
    // GET /Admin/Report/Index — trang chọn loại báo cáo và khoảng thời gian
    public IActionResult Index() => View();

    // GET /Admin/Report/ExportOrders?from=2026-01-01&to=2026-06-30&status=Paid
    public async Task<IActionResult> ExportOrders(
        DateTime? from, DateTime? to, string? status)
    {
        var fromUtc = (from?.ToUniversalTime() ?? DateTime.UtcNow.AddMonths(-1));
        var toUtc = (to?.ToUniversalTime() ?? DateTime.UtcNow).Date.AddDays(1).AddSeconds(-1);

        var query = db.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Where(o => o.OrderDate >= fromUtc && o.OrderDate <= toUtc);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(o => o.Status == status);

        var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();

        var sb = new StringBuilder();
        // BOM UTF-8 để Excel hiển thị tiếng Việt đúng
        sb.Append('\uFEFF');
        sb.AppendLine("Mã đơn,Ngày đặt,Khách hàng,Email,SĐT,Địa chỉ,Sản phẩm,Tổng tiền,Giảm giá,Mã giảm giá,PTTT,Trạng thái");

        foreach (var o in orders)
        {
            var products = string.Join(" | ", o.OrderItems.Select(i =>
                $"{i.Product?.ProductName} x{i.Quantity} ({i.PriceAtTime:N0}đ)"));

            sb.AppendLine(string.Join(",",
                $"#{o.Id:D8}",
                o.OrderDate.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                CsvEscape(o.User?.FullName ?? ""),
                CsvEscape(o.User?.Email ?? ""),
                o.Phone ?? "",
                CsvEscape(o.ShippingAddress ?? ""),
                CsvEscape(products),
                o.TotalAmount.ToString("N0"),
                o.DiscountAmount.ToString("N0"),
                o.CouponCode ?? "",
                o.PaymentMethod,
                o.Status
            ));
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"don-hang_{fromUtc:yyyyMMdd}_{toUtc:yyyyMMdd}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    // GET /Admin/Report/ExportRevenue?from=...&to=...
    // Doanh thu theo ngày (mỗi dòng = 1 ngày)
    public async Task<IActionResult> ExportRevenue(DateTime? from, DateTime? to)
    {
        var fromUtc = from?.ToUniversalTime() ?? DateTime.UtcNow.AddMonths(-1);
        var toUtc = (to?.ToUniversalTime() ?? DateTime.UtcNow).Date.AddDays(1).AddSeconds(-1);

        var raw = await db.Orders
            .Where(o => o.OrderDate >= fromUtc && o.OrderDate <= toUtc
                        && (o.Status == "Paid" || o.Status == "Shipping"))
            .Select(o => new { o.OrderDate, o.TotalAmount, o.DiscountAmount })
            .ToListAsync();

        var grouped = raw
            .GroupBy(o => o.OrderDate.ToLocalTime().Date)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                Date = g.Key,
                Orders = g.Count(),
                Revenue = g.Sum(x => x.TotalAmount),
                Discount = g.Sum(x => x.DiscountAmount)
            })
            .ToList();

        var sb = new StringBuilder();
        sb.Append('\uFEFF');
        sb.AppendLine("Ngày,Số đơn,Doanh thu (đ),Giảm giá (đ),Thực thu (đ)");

        foreach (var row in grouped)
        {
            sb.AppendLine($"{row.Date:dd/MM/yyyy},{row.Orders},{row.Revenue:N0},{row.Discount:N0},{(row.Revenue - row.Discount):N0}");
        }

        // Dòng tổng
        sb.AppendLine($"TỔNG,{grouped.Sum(r => r.Orders)},{grouped.Sum(r => r.Revenue):N0},{grouped.Sum(r => r.Discount):N0},{(grouped.Sum(r => r.Revenue) - grouped.Sum(r => r.Discount)):N0}");

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"doanh-thu_{fromUtc:yyyyMMdd}_{toUtc:yyyyMMdd}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    // GET /Admin/Report/ExportProducts — sản phẩm bán chạy
    public async Task<IActionResult> ExportProducts(DateTime? from, DateTime? to)
    {
        var fromUtc = from?.ToUniversalTime() ?? DateTime.UtcNow.AddMonths(-1);
        var toUtc = (to?.ToUniversalTime() ?? DateTime.UtcNow).Date.AddDays(1).AddSeconds(-1);

        var data = await db.OrderItems
            .Include(oi => oi.Product).ThenInclude(p => p.Brand)
            .Include(oi => oi.Product).ThenInclude(p => p.Category)
            .Include(oi => oi.Order)
            .Where(oi => oi.Order.OrderDate >= fromUtc && oi.Order.OrderDate <= toUtc
                         && (oi.Order.Status == "Paid" || oi.Order.Status == "Shipping"))
            .GroupBy(oi => new
            {
                oi.ProductId,
                Name = oi.Product.ProductName,
                Brand = oi.Product.Brand.Name,
                Category = oi.Product.Category.Name
            })
            .Select(g => new
            {
                g.Key.Name,
                g.Key.Brand,
                g.Key.Category,
                Sold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.Quantity * x.PriceAtTime)
            })
            .OrderByDescending(x => x.Sold)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.Append('\uFEFF');
        sb.AppendLine("Sản phẩm,Thương hiệu,Danh mục,Số lượng bán,Doanh thu (đ)");

        foreach (var row in data)
            sb.AppendLine($"{CsvEscape(row.Name)},{row.Brand},{row.Category},{row.Sold},{row.Revenue:N0}");

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv; charset=utf-8", $"san-pham-ban-chay_{fromUtc:yyyyMMdd}_{toUtc:yyyyMMdd}.csv");
    }

    // Escape chuỗi cho CSV: nếu có dấu phẩy hoặc xuống dòng → bọc trong ""
    private static string CsvEscape(string s)
    {
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
            return $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }
}