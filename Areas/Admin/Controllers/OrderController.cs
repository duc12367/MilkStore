
// FILE: Areas/Admin/Controllers/OrderController.cs
// MỤC ĐÍCH: Quản lý đơn hàng từ phía admin.
//           Gồm: xem danh sách (lọc + phân trang), cập nhật trạng thái,
//           xem chi tiết đơn, và reply bình luận sản phẩm.
//
// BẢO MẬT:
//   [Area("Admin")] + [AdminOnly] → chỉ admin mới truy cập được.
//   AdminOnly filter kiểm tra session role, người thường không vào được.


using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkStore.Filters;
using MilkStore.Models;

namespace MilkStore.Areas.Admin.Controllers;

/// <summary>
/// Controller quản lý đơn hàng dành cho admin.
/// Chỉ admin đăng nhập mới truy cập được (bảo vệ bởi [AdminOnly]).
/// </summary>
[Area("Admin")]
[AdminOnly]
public class OrderController(MilkStore4Context db) : Controller
{
    // --------------------------------------------------------
    // GET /Admin/Order/Index?status=Pending&page=1
    // Danh sách đơn hàng với lọc theo trạng thái và phân trang.
    //
    // PHÂN TRANG:
    //   pageSize = 10 → mỗi trang 10 đơn
    //   Skip((page-1) * pageSize).Take(pageSize) → lấy đúng trang
    //   TotalPages = ceil(total / pageSize)
    //
    // LỌC:
    //   status null → lấy tất cả
    //   status có giá trị → WHERE Status = @status
    // --------------------------------------------------------
    /// <summary>
    /// Danh sách đơn hàng. Hỗ trợ lọc theo trạng thái và phân trang 10 đơn/trang.
    /// </summary>
    public async Task<IActionResult> Index(string? status, int page = 1)
    {
        int pageSize = 10;

        var query = db.Orders
            .Include(o => o.User)        // Lấy tên khách hàng
            .Include(o => o.OrderItems)  // Lấy số lượng sản phẩm để hiển thị trong danh sách
            .AsQueryable();              // Giữ dạng IQueryable để chắp thêm điều kiện

        // Lọc theo trạng thái nếu admin chọn tab cụ thể
        if (!string.IsNullOrEmpty(status))
            query = query.Where(o => o.Status == status);

        // Đếm tổng để tính số trang (thực hiện COUNT trước khi Skip/Take)
        int total = await query.CountAsync();

        var orders = await query
            .OrderByDescending(o => o.OrderDate) // Đơn mới nhất lên đầu
            .Skip((page - 1) * pageSize)          // Bỏ qua các trang trước
            .Take(pageSize)                       // Lấy đúng 10 đơn
            .ToListAsync();

        ViewBag.Status     = status;
        ViewBag.Page       = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);

        return View(orders);
    }

    // --------------------------------------------------------
    // POST /Admin/Order/UpdateStatus
    // Admin cập nhật trạng thái đơn hàng (Pending → Shipping, v.v.)
    //
    // VALIDATE WHITELIST:
    //   Chỉ cho phép 4 trạng thái hợp lệ.
    //   Nếu truyền status không hợp lệ → trả về 400 BadRequest ngay.
    //   Tránh admin bị lừa cập nhật status tùy ý qua form giả.
    // --------------------------------------------------------
    /// <summary>
    /// Cập nhật trạng thái đơn hàng. Chỉ chấp nhận các trạng thái hợp lệ.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int orderId, string status)
    {
        // Whitelist trạng thái hợp lệ — từ chối mọi giá trị khác
        var validStatuses = new[] { "Pending", "Paid", "Shipping", "Cancelled" };
        if (!validStatuses.Contains(status))
            return BadRequest("Trạng thái không hợp lệ.");

        var order = await db.Orders.FindAsync(orderId);
        if (order == null) return NotFound();

        order.Status = status;
        await db.SaveChangesAsync();

        TempData["Success"] = $"Đã cập nhật đơn #{orderId} → {status}.";

        // Giữ nguyên tab lọc hiện tại sau khi cập nhật (UX tốt hơn)
        return RedirectToAction("Index", new { status = ViewBag.Status });
    }

    // --------------------------------------------------------
    // GET /Admin/Order/Detail/5
    // Chi tiết đơn hàng: thông tin khách, danh sách sản phẩm, giá, trạng thái.
    // ThenInclude(oi => oi.Product) để lấy tên và ảnh từng sản phẩm.
    // --------------------------------------------------------
    /// <summary>Chi tiết đơn hàng — xem đầy đủ thông tin khách và sản phẩm.</summary>
    public async Task<IActionResult> Detail(int id)
    {
        var order = await db.Orders
            .Include(o => o.User)        // Thông tin khách hàng
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product) // Tên, ảnh từng sản phẩm
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();
        return View(order);
    }

    // --------------------------------------------------------
    // POST /Admin/Order/ReplyReview
    // Admin reply bình luận của khách về sản phẩm.
    //
    // CƠ CHẾ:
    //   Tạo một Review mới với IsAdminReply = true và ParentReviewId = reviewId.
    //   → Phía View sẽ hiển thị reply thụt vào dưới bình luận gốc.
    //
    // LƯU Ý:
    //   adminUserId lấy từ session (tài khoản admin đang đăng nhập).
    //   Rating = 5 là giá trị mặc định vì admin không cần chấm điểm.
    // --------------------------------------------------------
    /// <summary>
    /// Admin phản hồi một bình luận sản phẩm của khách.
    /// Tạo Review mới có IsAdminReply = true, lồng dưới reviewId gốc.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReplyReview(int reviewId, string comment, int productId)
    {
        // Lấy UserId admin từ session (mặc định 1 nếu session hết hạn)
        var adminUserId = HttpContext.Session.GetInt32("UserId") ?? 1;

        db.Reviews.Add(new Review
        {
            UserId        = adminUserId,
            ProductId     = productId,
            Rating        = 5,              // Admin không cần chấm sao
            Comment       = comment,
            CreatedAt     = DateTime.UtcNow,
            ParentReviewId = reviewId,      // Lồng dưới bình luận gốc
            IsAdminReply  = true            // Đánh dấu để View hiển thị khác (badge Admin)
        });

        await db.SaveChangesAsync();

        // Redirect về trang chi tiết sản phẩm để xem reply vừa tạo
        return RedirectToAction("Detail", new { id = productId });
    }
}
