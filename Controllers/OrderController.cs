
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkStore.Filters;
using MilkStore.Models;

namespace MilkStore.Controllers;

[LoginRequired]
public class OrderController(MilkStore4Context db) : Controller
{
    private int? UserIdNullable => HttpContext.Session.GetInt32("UserId");
    private int UserId => HttpContext.Session.GetInt32("UserId")!.Value;

    // GET: /Order/Checkout
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

        var user = await db.Users.FindAsync(UserId);

        ViewBag.Items = items;
        ViewBag.Total = items.Sum(c => (c.Product?.Price ?? 0m) * c.Quantity);
        ViewBag.DefaultAddress = user?.Address ?? "";

        return View();
    }

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

        decimal total = items.Sum(c => (c.Product?.Price ?? 0m) * c.Quantity);

        var order = new Order
        {
            UserId = UserId,
            OrderDate = DateTime.Now,
            TotalAmount = total,
            Status = "Pending",
            PaymentMethod = paymentMethod,
            ShippingAddress = shippingAddress,
            Note = note
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        foreach (var item in items)
        {
            db.OrderItems.Add(new OrderItem
            {
                OrderId = order.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                PriceAtTime = item.Product?.Price ?? 0m
            });

            var product = await db.Products.FindAsync(item.ProductId);
            if (product != null)
                product.StockQuantity = Math.Max(0,
                    product.StockQuantity - item.Quantity);
        }

        db.CartItems.RemoveRange(items);
        await db.SaveChangesAsync();

        if (paymentMethod == "VNPay" || paymentMethod == "MoMo")
            return RedirectToAction("CreatePayment", "Payment", new { orderId = order.Id });

        return RedirectToAction("Success", new { id = order.Id });
    }

    // GET: /Order/Success/5
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

    // GET: /Order/MyOrders
    public async Task<IActionResult> MyOrders()
    {
        if (UserIdNullable == null)
            return RedirectToAction("Login", "Account");

        // ✅ FIX: Include Product để lấy ảnh và tên sản phẩm trong OrderItems
        var orders = await db.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Where(o => o.UserId == UserId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return View(orders);
    }

    // GET: /Order/Detail/5
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

    // ✅ FIX MỚI: POST /Order/CancelOrder - Cho phép khách hủy đơn Pending
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

        // Chỉ hủy được đơn đang Pending
        if (order.Status != "Pending")
        {
            TempData["Error"] = "Chỉ có thể hủy đơn hàng đang chờ xử lý.";
            return RedirectToAction("MyOrders");
        }

        order.Status = "Cancelled";

        // Hoàn lại tồn kho
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