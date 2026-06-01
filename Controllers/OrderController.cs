// FILE: Controllers/OrderController.cs
// MỤC ĐÍCH: Xử lý toàn bộ luồng đặt hàng của khách hàng.
//
// LUỒNG CHÍNH (Happy Path):
//   1. Khách vào /Order/Checkout → xem giỏ hàng + điền địa chỉ
//   2. Bấm "Đặt hàng" → POST PlaceOrder
//   3. PlaceOrder: tạo Order + OrderItems, trừ kho (nguyên tử), xóa giỏ
//   4a. Nếu COD → redirect thẳng đến trang Success
//   4b. Nếu MoMo/VNPay → redirect đến PaymentController để thanh toán online
//
// FIX NGẮN HẠN:
//   [TX]  PlaceOrder bọc trong một transaction duy nhất — rollback toàn bộ nếu có lỗi.
//   [RC]  Trừ kho bằng ExecuteUpdateAsync nguyên tử (WHERE StockQuantity >= qty)
//         → tránh race condition khi 2 user cùng mua sản phẩm cuối.
//   [UTC] Toàn bộ DateTime dùng DateTime.UtcNow nhất quán.

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkStore.Filters;
using MilkStore.Models;
using MilkStore.Services;

namespace MilkStore.Controllers;

[LoginRequired]
public class OrderController(MilkStore4Context db, EmailService emailSvc) : Controller
{
    private int? UserIdNullable => HttpContext.Session.GetInt32("UserId");
    private int UserId => HttpContext.Session.GetInt32("UserId")!.Value;

    // ────────────────────────────────────────────────────────
    // GET /Order/Checkout
    // ────────────────────────────────────────────────────────
    public async Task<IActionResult> Checkout()
    {
        if (UserIdNullable == null)
            return RedirectToAction("Login", "Account");

        var items = await db.CartItems
            .Include(c => c.Product)
            .Where(c => c.UserId == UserId)
            .ToListAsync();

        if (!items.Any())
            return RedirectToAction("Index", "Cart");

        // [UTC] dùng UtcNow nhất quán
        var now = DateTime.UtcNow;
        ViewBag.ActiveCoupons = await db.Coupons
            .Where(c => c.StartDate <= now && c.ExpiryDate >= now &&
                        (c.MaxUsage == null || c.UsageCount < c.MaxUsage))
            .OrderBy(c => c.Code)
            .ToListAsync();

        var user = await db.Users.FindAsync(UserId);

        ViewBag.Items = items;
        ViewBag.Total = items.Sum(c => (c.Product?.Price ?? 0m) * c.Quantity);
        ViewBag.DefaultAddress = user?.Address ?? "";
        ViewBag.DefaultPhone = user?.Phone ?? "";
        ViewBag.DefaultEmail = user?.Email ?? "";

        return View();
    }

    // ────────────────────────────────────────────────────────
    // POST /Order/PlaceOrder
    //
    // Pipeline (tất cả trong 1 transaction):
    //   B1: Validate input
    //   B2: Load giỏ hàng, kiểm tra tồn kho sơ bộ
    //   B3: Tính tiền, áp mã giảm giá
    //   B4: Tạo Order → lấy Order.Id
    //   B5: Trừ kho nguyên tử (ExecuteUpdateAsync) — rollback nếu hết hàng
    //   B6: Tạo OrderItems + xóa giỏ
    //   B7: Commit transaction
    //   B8: Gửi email (ngoài transaction, không ảnh hưởng đơn hàng)
    // ────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(
        string shippingAddress, string paymentMethod,
        string? note, string? phone, string? email, string? couponCode)
    {
        if (UserIdNullable == null)
            return RedirectToAction("Login", "Account");

        // ── B1: Validate input ───────────────────────────────
        if (string.IsNullOrWhiteSpace(shippingAddress))
        {
            TempData["Error"] = "Vui lòng nhập địa chỉ giao hàng.";
            return RedirectToAction("Checkout");
        }
        if (shippingAddress.Trim().Length < 10)
        {
            TempData["Error"] = "Địa chỉ giao hàng quá ngắn (tối thiểu 10 ký tự).";
            return RedirectToAction("Checkout");
        }
        if (string.IsNullOrWhiteSpace(phone))
        {
            TempData["Error"] = "Vui lòng nhập số điện thoại.";
            return RedirectToAction("Checkout");
        }
        if (!System.Text.RegularExpressions.Regex.IsMatch(phone.Trim(), @"^[0-9]{10,11}$"))
        {
            TempData["Error"] = "Số điện thoại không hợp lệ (10–11 chữ số).";
            return RedirectToAction("Checkout");
        }
        if (!string.IsNullOrWhiteSpace(email) &&
            !System.Text.RegularExpressions.Regex.IsMatch(email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            TempData["Error"] = "Email không đúng định dạng.";
            return RedirectToAction("Checkout");
        }

        // ── B2: Load giỏ hàng ───────────────────────────────
        var items = await db.CartItems
            .Include(c => c.Product)
            .Where(c => c.UserId == UserId)
            .ToListAsync();

        if (!items.Any())
            return RedirectToAction("Index", "Cart");

        // Kiểm tra sơ bộ tồn kho (đọc nhanh, không lock)
        var outOfStock = items
            .Where(c => c.Product != null && c.Quantity > c.Product.StockQuantity)
            .Select(c => c.Product!.ProductName)
            .ToList();

        if (outOfStock.Any())
        {
            TempData["Error"] = $"Sản phẩm không đủ hàng: {string.Join(", ", outOfStock)}.";
            return RedirectToAction("Index", "Cart");
        }

        // ── B3: Tính tiền + áp coupon ───────────────────────
        decimal total = items.Sum(c => (c.Product?.Price ?? 0m) * c.Quantity);
        decimal discountAmount = 0;
        string? appliedCouponCode = null;
        Coupon? coupon = null;

        if (!string.IsNullOrWhiteSpace(couponCode))
        {
            var code = couponCode.Trim().ToUpper();
            coupon = await db.Coupons.FirstOrDefaultAsync(c => c.Code == code);
            var now = DateTime.UtcNow;

            if (coupon == null) { TempData["Error"] = "Mã giảm giá không tồn tại."; return RedirectToAction("Checkout"); }
            if (now < coupon.StartDate) { TempData["Error"] = $"Mã chưa có hiệu lực. Bắt đầu từ {coupon.StartDate:dd/MM/yyyy}."; return RedirectToAction("Checkout"); }
            if (now > coupon.ExpiryDate) { TempData["Error"] = "Mã giảm giá đã hết hạn."; return RedirectToAction("Checkout"); }
            if (coupon.MaxUsage.HasValue && coupon.UsageCount >= coupon.MaxUsage.Value) { TempData["Error"] = "Mã đã hết lượt sử dụng."; return RedirectToAction("Checkout"); }

            discountAmount = coupon.DiscountType == "Percent"
                ? total * coupon.DiscountValue / 100
                : coupon.DiscountValue;
            discountAmount = Math.Min(discountAmount, total);
            total = total - discountAmount;
            appliedCouponCode = code;
        }

        // ── B4–B7: Tất cả DB trong 1 transaction ────────────
        // [TX] Nếu bất kỳ bước nào lỗi → rollback toàn bộ,
        //      không để Order mồ côi (không có OrderItems).
        int orderId;
        using (var tx = await db.Database.BeginTransactionAsync())
        {
            try
            {
                // B4: Tạo Order
                var order = new Order
                {
                    UserId = UserId,
                    OrderDate = DateTime.UtcNow,  // [UTC]
                    TotalAmount = total,
                    Status = "Pending",
                    PaymentMethod = paymentMethod,
                    ShippingAddress = shippingAddress.Trim(),
                    Phone = phone.Trim(),
                    Note = note,
                    CouponCode = appliedCouponCode,
                    DiscountAmount = discountAmount
                };
                db.Orders.Add(order);
                await db.SaveChangesAsync(); // lấy order.Id
                orderId = order.Id;

                // B5: [RC] Trừ kho nguyên tử — UPDATE với điều kiện StockQuantity >= qty
                //     Nếu affected rows < số sản phẩm → hàng đã hết giữa chừng → rollback
                foreach (var item in items)
                {
                    int affected = await db.Products
                        .Where(p => p.Id == item.ProductId && p.StockQuantity >= item.Quantity)
                        .ExecuteUpdateAsync(s => s.SetProperty(
                            p => p.StockQuantity,
                            p => p.StockQuantity - item.Quantity));

                    if (affected == 0)
                    {
                        // Hàng vừa hết (race condition) → rollback, báo lỗi
                        await tx.RollbackAsync();
                        TempData["Error"] = $"Sản phẩm \"{item.Product?.ProductName}\" vừa hết hàng. Vui lòng kiểm tra lại giỏ hàng.";
                        return RedirectToAction("Index", "Cart");
                    }
                }

                // B6: Tạo OrderItems + tăng UsageCount coupon + xóa giỏ
                foreach (var item in items)
                {
                    db.OrderItems.Add(new OrderItem
                    {
                        OrderId = orderId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        PriceAtTime = item.Product?.Price ?? 0m
                    });
                }

                if (coupon != null)
                    coupon.UsageCount += 1;  // trong transaction → tự rollback nếu lỗi

                db.CartItems.RemoveRange(items);
                await db.SaveChangesAsync();

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // ── B8: Gửi email (ngoài transaction) ───────────────
        try
        {
            var user = await db.Users.FindAsync(UserId);
            var itemDetails = items.Select(i => (
                i.Product?.ProductName ?? "Sản phẩm",
                i.Quantity,
                i.Product?.Price ?? 0m
            )).ToList();

            var emailTasks = new List<Task>();

            if (!string.IsNullOrWhiteSpace(user?.Email))
                emailTasks.Add(emailSvc.SendOrderConfirmationAsync(
                    user.Email, user.FullName ?? "Khách hàng",
                    orderId, total, shippingAddress, itemDetails));

            emailTasks.Add(emailSvc.SendNewOrderNotifyAdminAsync(
                orderId,
                (await db.Users.FindAsync(UserId))?.FullName ?? "Khách hàng",
                (await db.Users.FindAsync(UserId))?.Email ?? "(không có email)",
                total, shippingAddress, phone ?? ""));

            await Task.WhenAll(emailTasks);
        }
        catch (Exception ex)
        {
            HttpContext.RequestServices
                .GetRequiredService<ILogger<OrderController>>()
                .LogError(ex, "[EMAIL] Lỗi gửi email cho đơn #{OrderId}", orderId);
        }

        if (paymentMethod == "VNPay" || paymentMethod == "MoMo")
            return RedirectToAction("CreatePayment", "Payment", new { orderId });

        return RedirectToAction("Success", new { id = orderId });
    }

    // ────────────────────────────────────────────────────────
    // GET /Order/Success/5
    // ────────────────────────────────────────────────────────
    public async Task<IActionResult> Success(int id)
    {
        if (UserIdNullable == null)
            return RedirectToAction("Login", "Account");

        var order = await db.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == UserId);

        if (order == null) return NotFound();
        return View(order);
    }

    // ────────────────────────────────────────────────────────
    // GET /Order/MyOrders
    // ────────────────────────────────────────────────────────
    public async Task<IActionResult> MyOrders()
    {
        if (UserIdNullable == null)
            return RedirectToAction("Login", "Account");

        var orders = await db.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Where(o => o.UserId == UserId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return View(orders);
    }

    // ────────────────────────────────────────────────────────
    // GET /Order/Detail/5
    // ────────────────────────────────────────────────────────
    public async Task<IActionResult> Detail(int id)
    {
        if (UserIdNullable == null)
            return RedirectToAction("Login", "Account");

        var order = await db.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == UserId);

        if (order == null) return NotFound();
        return View(order);
    }

    // ────────────────────────────────────────────────────────
    // POST /Order/CancelOrder
    // Chỉ hủy được đơn "Pending". Hoàn kho trong transaction.
    // ────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelOrder(int id)
    {
        if (UserIdNullable == null)
            return RedirectToAction("Login", "Account");

        var order = await db.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == UserId);

        if (order == null) return NotFound();

        if (order.Status != "Pending")
        {
            TempData["Error"] = "Chỉ có thể hủy đơn hàng đang chờ xử lý.";
            return RedirectToAction("MyOrders");
        }

        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            order.Status = "Cancelled";

            // Hoàn kho nguyên tử
            foreach (var item in order.OrderItems)
            {
                await db.Products
                    .Where(p => p.Id == item.ProductId)
                    .ExecuteUpdateAsync(s => s.SetProperty(
                        p => p.StockQuantity,
                        p => p.StockQuantity + item.Quantity));
            }

            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        TempData["Success"] = "Đã hủy đơn hàng thành công.";
        return RedirectToAction("MyOrders");
    }

    // ── GET /Order/PreviewCoupon (AJAX) ─────────────────────
    [HttpGet]
    public async Task<IActionResult> PreviewCoupon(string code, decimal total)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Json(new { error = "Vui lòng nhập mã giảm giá." });

        code = code.Trim().ToUpper();
        var coupon = await db.Coupons.FirstOrDefaultAsync(c => c.Code == code);
        var now = DateTime.UtcNow;

        if (coupon == null) return Json(new { error = "Mã không tồn tại." });
        if (now < coupon.StartDate) return Json(new { error = $"Mã chưa có hiệu lực, bắt đầu từ {coupon.StartDate:dd/MM/yyyy}." });
        if (now > coupon.ExpiryDate) return Json(new { error = "Mã đã hết hạn." });
        if (coupon.MaxUsage.HasValue && coupon.UsageCount >= coupon.MaxUsage.Value) return Json(new { error = "Mã đã hết lượt sử dụng." });

        decimal discount = coupon.DiscountType == "Percent"
            ? total * coupon.DiscountValue / 100
            : coupon.DiscountValue;
        discount = Math.Min(discount, total);

        string label = coupon.DiscountType == "Percent"
            ? $"{coupon.DiscountValue}%"
            : $"{coupon.DiscountValue:N0}đ";

        return Json(new
        {
            message = $"Áp dụng thành công mã {code}!",
            label,
            discountAmount = discount,
            finalTotal = total - discount
        });
    }
}