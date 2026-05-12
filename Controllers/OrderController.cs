// FILE: Controllers/OrderController.cs
// MỤC ĐÍCH: Xử lý toàn bộ luồng đặt hàng của khách hàng.
//           Bao gồm: xem trang checkout, đặt hàng (PlaceOrder),
//           xem đơn đã đặt, chi tiết đơn, và hủy đơn.
//
// LUỒNG CHÍNH (Happy Path):
//   1. Khách vào /Order/Checkout → xem giỏ hàng + điền địa chỉ
//   2. Bấm "Đặt hàng" → POST PlaceOrder
//   3. PlaceOrder: tạo Order + OrderItems, trừ kho, xóa giỏ
//   4a. Nếu COD → redirect thẳng đến trang Success
//   4b. Nếu MoMo/VNPay → redirect đến PaymentController để thanh toán online


using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkStore.Filters;
using MilkStore.Models;

namespace MilkStore.Controllers;


/// Controller xử lý đặt hàng và quản lý lịch sử đơn hàng của khách.
/// Tất cả action yêu cầu đăng nhập (bảo vệ bởi [LoginRequired]).

[LoginRequired]
public class OrderController(MilkStore4Context db) : Controller
{
    // Hai cách lấy UserId — dùng Nullable khi cần kiểm tra null trước redirect
    private int? UserIdNullable => HttpContext.Session.GetInt32("UserId");
    private int UserId => HttpContext.Session.GetInt32("UserId")!.Value;

    // --------------------------------------------------------
    // GET /Order/Checkout
    // Hiển thị trang xác nhận đơn hàng trước khi đặt.
    // Kiểm tra: nếu giỏ rỗng → redirect về giỏ hàng.
    // Điền sẵn địa chỉ mặc định từ tài khoản user (có thể sửa).
    // --------------------------------------------------------
    /// Trang checkout — xem lại giỏ hàng và điền thông tin giao hàng.
    public async Task<IActionResult> Checkout()
    {
        if (UserIdNullable == null)
            return RedirectToAction("Login", "Account");

        var items = await db.CartItems
            .Include(c => c.Product)
            .Where(c => c.UserId == UserId)
            .ToListAsync();

        // Giỏ rỗng → không cho vào checkout
        if (!items.Any())
            return RedirectToAction("Index", "Cart");

        var user = await db.Users.FindAsync(UserId);

        ViewBag.Items = items;
        ViewBag.Total = items.Sum(c => (c.Product?.Price ?? 0m) * c.Quantity);
        ViewBag.DefaultAddress = user?.Address ?? "";  // Điền sẵn địa chỉ tài khoản

        return View();
    }

    // --------------------------------------------------------
    // POST /Order/PlaceOrder
    // ĐÂY LÀ ACTION TRUNG TÂM — thực hiện đặt hàng thực sự.
    //
    // PIPELINE XỬ LÝ (5 bước, thực hiện trong 2 lần SaveChanges):
    //
    //   [SaveChanges lần 1] — lấy Order.Id từ DB
    //     Bước 1: Tính tổng tiền từ giỏ hàng
    //     Bước 2: Tạo đối tượng Order (trạng thái "Pending") và lưu DB
    //             → cần Order.Id trước khi tạo OrderItems
    //
    //   [SaveChanges lần 2] — ghi tất cả thay đổi còn lại
    //     Bước 3: Tạo OrderItem cho từng sản phẩm (snapshot giá hiện tại)
    //     Bước 4: Trừ tồn kho (Math.Max tránh âm)
    //     Bước 5: Xóa toàn bộ giỏ hàng
    //
    //   Sau đó:
    //     → COD: redirect thẳng đến Success
    //     → MoMo/VNPay: redirect đến PaymentController.CreatePayment
    //
    // LƯU Ý VỀ TRANSACTION:
    //   Hiện tại không dùng explicit Transaction — nếu SaveChanges lần 2 lỗi
    //   thì Order đã tạo nhưng chưa có OrderItems (dữ liệu không nhất quán).
    //   Cải thiện sau: bọc toàn bộ trong using var tx = db.Database.BeginTransaction()
    // --------------------------------------------------------
    /// <summary>
    /// Đặt hàng: tạo Order, snapshot giá, trừ kho, xóa giỏ,
    /// rồi redirect theo phương thức thanh toán.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(string shippingAddress,
        string paymentMethod, string? note)
    {
        if (UserIdNullable == null)
            return RedirectToAction("Login", "Account");

        var items = await db.CartItems
            .Include(c => c.Product)
            .Where(c => c.UserId == UserId)
            .ToListAsync();

        if (!items.Any())
            return RedirectToAction("Index", "Cart");

        // *** KIỂM TRA TỒN KHO TRƯỚC KHI ĐẶT HÀNG ***
        // Nếu bất kỳ sản phẩm nào SL mua > SL tồn kho → báo lỗi, không tạo đơn
        var outOfStockItems = items
            .Where(c => c.Product != null && c.Quantity > c.Product.StockQuantity)
            .Select(c => c.Product!.ProductName)
            .ToList();

        if (outOfStockItems.Any())
        {
            TempData["Error"] = $"Sản phẩm sau không đủ số lượng tồn kho: {string.Join(", ", outOfStockItems)}. Vui lòng kiểm tra lại giỏ hàng.";
            return RedirectToAction("Index", "Cart");
        }

        // Bước 1: Tính tổng tiền (snapshot tại thời điểm đặt)
        decimal total = items.Sum(c => (c.Product?.Price ?? 0m) * c.Quantity);

        // Bước 2: Tạo Order và lưu lần 1 để lấy Order.Id tự tăng
        var order = new Order
        {
            UserId = UserId,
            OrderDate = DateTime.UtcNow,
            TotalAmount = total,
            Status = "Pending",          // Trạng thái ban đầu luôn là Pending
            PaymentMethod = paymentMethod,
            ShippingAddress = shippingAddress,
            Note = note
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();     // Lần 1: lấy Order.Id từ DB

        // Bước 3 & 4: Tạo OrderItems + trừ tồn kho
        foreach (var item in items)
        {
            // Snapshot giá: lưu PriceAtTime để giá đơn hàng không bị thay đổi
            // dù admin cập nhật giá sản phẩm sau này
            db.OrderItems.Add(new OrderItem
            {
                OrderId = order.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                PriceAtTime = item.Product?.Price ?? 0m   // Giá tại thời điểm đặt
            });

            // Trừ tồn kho — Math.Max(0, ...) đảm bảo không bao giờ âm
            // (trường hợp race condition: 2 người cùng mua hàng cuối)
            var product = await db.Products.FindAsync(item.ProductId);
            if (product != null)
                product.StockQuantity = Math.Max(0,
                    product.StockQuantity - item.Quantity);
        }

        // Bước 5: Xóa toàn bộ giỏ hàng sau khi đặt thành công
        db.CartItems.RemoveRange(items);
        await db.SaveChangesAsync();    // Lần 2: lưu OrderItems + trừ kho + xóa giỏ

        // Phân luồng theo phương thức thanh toán
        if (paymentMethod == "VNPay" || paymentMethod == "MoMo")
            // Thanh toán online → chuyển sang PaymentController
            return RedirectToAction("CreatePayment", "Payment", new { orderId = order.Id });

        // Thanh toán COD → thành công ngay
        return RedirectToAction("Success", new { id = order.Id });
    }

    // --------------------------------------------------------
    // GET /Order/Success/5
    // Trang xác nhận đặt hàng thành công.
    // Load full đơn hàng kèm sản phẩm để hiển thị tóm tắt.
    // Bảo mật: lọc o.UserId == UserId để không xem được đơn người khác.
    // --------------------------------------------------------
    /// <summary>Trang thông báo đặt hàng / thanh toán thành công.</summary>
    public async Task<IActionResult> Success(int id)
    {
        if (UserIdNullable == null)
            return RedirectToAction("Login", "Account");

        var order = await db.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == UserId); // Bảo mật: chỉ xem đơn của mình

        if (order == null) return NotFound();
        return View(order);
    }

    // --------------------------------------------------------
    // GET /Order/MyOrders
    // Lịch sử toàn bộ đơn hàng của user, sắp xếp mới nhất lên đầu.
    // ThenInclude(oi => oi.Product) để lấy ảnh và tên sản phẩm
    // hiển thị thumbnail trong danh sách đơn hàng.
    // --------------------------------------------------------
    /// <summary>Lịch sử đơn hàng của user hiện tại, sắp xếp mới nhất lên đầu.</summary>
    public async Task<IActionResult> MyOrders()
    {
        if (UserIdNullable == null)
            return RedirectToAction("Login", "Account");

        var orders = await db.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)  // Cần để hiển thị ảnh và tên SP trong danh sách
            .Where(o => o.UserId == UserId)
            .OrderByDescending(o => o.OrderDate) // Đơn mới nhất lên đầu
            .ToListAsync();

        return View(orders);
    }

    // --------------------------------------------------------
    // GET /Order/Detail/5
    // Chi tiết một đơn hàng cụ thể.
    // --------------------------------------------------------
    /// <summary>Chi tiết đơn hàng — xem đầy đủ sản phẩm, giá, trạng thái.</summary>
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

    // --------------------------------------------------------
    // POST /Order/CancelOrder
    // Cho phép khách tự hủy đơn — chỉ khi trạng thái là "Pending".
    // Đơn đang giao hoặc đã thanh toán KHÔNG được hủy từ phía khách.
    //
    // Khi hủy: hoàn lại tồn kho cho từng sản phẩm trong đơn.
    // --------------------------------------------------------
    /// <summary>
    /// Khách hủy đơn hàng. Chỉ áp dụng với đơn "Pending".
    /// Tự động hoàn tồn kho khi hủy thành công.
    /// </summary>
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

        // Chặn hủy nếu đơn đã qua trạng thái Pending (đang giao, đã thanh toán...)
        if (order.Status != "Pending")
        {
            TempData["Error"] = "Chỉ có thể hủy đơn hàng đang chờ xử lý.";
            return RedirectToAction("MyOrders");
        }

        order.Status = "Cancelled";

        // Hoàn lại tồn kho: cộng ngược số lượng đã trừ lúc PlaceOrder
        foreach (var item in order.OrderItems)
        {
            if (item.Product != null)
                item.Product.StockQuantity += item.Quantity;
        }

        await db.SaveChangesAsync();
        TempData["Success"] = "Đã hủy đơn hàng thành công.";
        return RedirectToAction("MyOrders");
    }
}