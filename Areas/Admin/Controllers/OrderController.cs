// FILE: Areas/Admin/Controllers/OrderController.cs
// MỤC ĐÍCH: Quản lý đơn hàng từ phía admin.


using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkStore.Filters;
using MilkStore.Models;
using System.Text.RegularExpressions;

namespace MilkStore.Areas.Admin.Controllers;

[Area("Admin")]
[AdminOnly]
public class OrderController(MilkStore4Context db, Microsoft.AspNetCore.SignalR.IHubContext<MilkStore.Hubs.NotificationHub> notifHub) : Controller
{
    // ── Trạng thái hợp lệ (whitelist dùng chung) ────────────
    private static readonly string[] ValidStatuses =
        ["Pending", "Paid", "Shipping", "Cancelled"];

    // ── Trạng thái được phép xóa (đơn đã hoàn tất hoặc đã hủy) ──
    private static readonly string[] DeletableStatuses = ["Cancelled", "Failed"];

    // ── Trạng thái KHÔNG được hủy (đang giao hoặc đã xong) ──
    private static readonly string[] NonCancellableStatuses = ["Shipping", "Failed"];


    // GET /Admin/Order/Index?status=Pending&search=...&page=1
    //
    //             : search "#MS00000000018" → strip "#MS" → parse "18" → tìm Id=18.
    //            Trước đây chỉ parse số thuần, nên "#MS..." không match được.
    //              Sanitize search — xóa ký tự đặc biệt không hợp lệ,
    //            giới hạn 100 ký tự để tránh DoS / crash DB query.
  
    public async Task<IActionResult> Index(string? status, string? search, int page = 1)
    {
        int pageSize = 10;

        // : giới hạn 100 ký tự, xóa ký tự điều khiển
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            if (search.Length > 100) search = search[..100];
            // Xóa ký tự điều khiển (null bytes, escape sequences, ...) nhưng giữ @, #, -, /
            search = Regex.Replace(search, @"[\x00-\x1F\x7F]", "");
            if (string.IsNullOrWhiteSpace(search)) search = null;
        }

        var query = db.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(o => o.Status == status);

        if (!string.IsNullOrWhiteSpace(search))
        {
            //  nhận dạng mã đơn dạng "#MS00000000018" hoặc "MS18" hoặc "18"
            var normalized = search.Trim();
            var msMatch = Regex.Match(normalized, @"(?:#?MS0*)(\d+)", RegexOptions.IgnoreCase);
            if (msMatch.Success && int.TryParse(msMatch.Groups[1].Value, out int msOrderId))
            {
                query = query.Where(o => o.Id == msOrderId);
            }
            else if (int.TryParse(normalized, out int orderId))
            {
                // Nhập số thuần (ví dụ: "18")
                query = query.Where(o => o.Id == orderId);
            }
            else
            {
                // Tìm theo tên, SĐT, địa chỉ
                var s = normalized.ToLower();
                query = query.Where(o =>
                    o.User!.FullName.ToLower().Contains(s) ||
                    (o.Phone != null && o.Phone.Contains(s)) ||
                    (o.ShippingAddress != null && o.ShippingAddress.ToLower().Contains(s)));
            }
        }

        try
        {
            int total = await query.CountAsync();

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Status = status;
            ViewBag.Search = search;
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);

            return View(orders);
        }
        catch (Exception)
        {
            //  mất kết nối DB → trang lỗi thân thiện thay vì crash 500
            TempData["Error"] = "Không thể kết nối cơ sở dữ liệu. Vui lòng thử lại sau.";
            return View(new List<Order>());
        }
    }

 
    // POST /Admin/Order/UpdateStatus
    //
    //  Không cho hủy đơn đang giao (Shipping) hoặc đã Failed.
    //            Trước đây chỉ validate whitelist, không kiểm tra
    //            transition logic → cho phép Shipping → Cancelled.

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int orderId, string status)
    {
        if (!ValidStatuses.Contains(status))
            return BadRequest("Trạng thái không hợp lệ.");

        try
        {
            var order = await db.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            // Chặn hủy đơn đang giao / đã thất bại
            if (status == "Cancelled" && NonCancellableStatuses.Contains(order.Status))
            {
                TempData["Error"] =
                    $"Không thể hủy đơn #{orderId}: đơn đang ở trạng thái \"{order.Status}\", không thể thay đổi.";
                return RedirectToAction("Index", new { status = ViewBag.Status });
            }

            order.Status = status;
            await db.SaveChangesAsync();

            // Gửi thông báo realtime cho khách khi đơn đổi trạng thái
            var notifMsg = status switch
            {
                "Paid" => $"✅ Đơn hàng #{orderId} của bạn đã được thanh toán.",
                "Shipping" => $"🚚 Đơn hàng #{orderId} đang được giao đến bạn!",
                "Cancelled" => $"❌ Đơn hàng #{orderId} đã bị hủy.",
                _ => null
            };
            if (notifMsg != null)
            {
                await MilkStore.Hubs.NotificationHub.NotifyUser(
                    notifHub, order.UserId, status, notifMsg,
                    $"/Order/Detail/{orderId}");
            }

            TempData["Success"] = $"Đã cập nhật đơn #{orderId} → {status}.";
            return RedirectToAction("Index", new { status = ViewBag.Status });
        }
        catch (Exception)
        {
            // lỗi DB khi save
            TempData["Error"] = "Lỗi hệ thống khi cập nhật đơn hàng. Vui lòng thử lại.";
            return RedirectToAction("Index");
        }
    }

    // GET /Admin/Order/Detail/5

    public async Task<IActionResult> Detail(int id)
    {
        try
        {
            var order = await db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();
            return View(order);
        }
        catch (Exception)
        {
            // [TC19]
            TempData["Error"] = "Không thể tải chi tiết đơn hàng. Vui lòng thử lại sau.";
            return RedirectToAction("Index");
        }
    }


    // POST /Admin/Order/Delete
    //
    //            Thêm mới action Delete.
    //            Chặn xóa đơn đang xử lý (Pending / Paid / Shipping).
    //            Chỉ cho phép xóa đơn đã Cancelled hoặc Failed.

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int orderId)
    {
        try
        {
            var order = await db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound();

            //  Chặn xóa đơn đang ở trạng thái xử lý
            if (!DeletableStatuses.Contains(order.Status))
            {
                TempData["Error"] =
                    $"Không thể xóa đơn #{orderId}: đơn đang ở trạng thái \"{order.Status}\". " +
                    "Chỉ có thể xóa đơn đã Cancelled hoặc Failed.";
                return RedirectToAction("Index");
            }

            // Xóa OrderItems trước (tránh lỗi FK), rồi xóa Order
            db.OrderItems.RemoveRange(order.OrderItems);
            db.Orders.Remove(order);
            await db.SaveChangesAsync();

            TempData["Success"] = $"Đã xóa đơn hàng #{orderId}.";
            return RedirectToAction("Index");
        }
        catch (Exception)
        {
           
            TempData["Error"] = "Lỗi hệ thống khi xóa đơn hàng. Vui lòng thử lại.";
            return RedirectToAction("Index");
        }
    }

    // POST /Admin/Order/ReplyReview

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReplyReview(int reviewId, string comment, int productId)
    {
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