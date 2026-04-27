
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkStore.Filters;
using MilkStore.Models;

namespace MilkStore.Areas.Admin.Controllers;

[Area("Admin")]
[AdminOnly]
public class OrderController(MilkStore4Context db) : Controller
{
    public async Task<IActionResult> Index(string? status, int page = 1)
    {
        int pageSize = 10;
        var query = db.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems)  // ✅ FIX: Include OrderItems để đếm số sản phẩm
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(o => o.Status == status);

        int total = await query.CountAsync();
        var orders = await query
            .OrderByDescending(o => o.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Status = status;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
        return View(orders);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int orderId, string status)
    {
        // ✅ FIX: Validate status hợp lệ
        var validStatuses = new[] { "Pending", "Paid", "Shipping", "Cancelled" };
        if (!validStatuses.Contains(status))
            return BadRequest("Trạng thái không hợp lệ.");

        var order = await db.Orders.FindAsync(orderId);
        if (order == null) return NotFound();

        order.Status = status;
        await db.SaveChangesAsync();

        TempData["Success"] = $"Đã cập nhật đơn #{orderId} → {status}.";
        return RedirectToAction("Index", new { status = ViewBag.Status });
    }

    public async Task<IActionResult> Detail(int id)
    {
        var order = await db.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();
        return View(order);
    }

    // ✅ MỚI: Admin reply bình luận
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReplyReview(int reviewId, string comment, int productId)
    {
        // Tìm admin user (ví dụ UserId = 1, hoặc lấy từ session admin)
        var adminUserId = HttpContext.Session.GetInt32("UserId") ?? 1;

        db.Reviews.Add(new Review
        {
            UserId = adminUserId,
            ProductId = productId,
            Rating = 5,
            Comment = comment,
            CreatedAt = DateTime.UtcNow,
            ParentReviewId = reviewId,
            IsAdminReply = true
        });

        await db.SaveChangesAsync();
        return RedirectToAction("Detail", new { id = productId });
    }
}