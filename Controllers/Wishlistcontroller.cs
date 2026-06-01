// FILE: Controllers/WishlistController.cs
// Quản lý danh sách yêu thích (Wishlist).
// API dạng AJAX — trả JSON → frontend cập nhật UI không reload trang.

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkStore.Filters;
using MilkStore.Models;

namespace MilkStore.Controllers;

[LoginRequired]
public class WishlistController(MilkStore4Context db) : Controller
{
    private int UserId => HttpContext.Session.GetInt32("UserId")!.Value;

    // GET /Wishlist — trang danh sách yêu thích
    public async Task<IActionResult> Index()
    {
        var items = await db.WishlistItems
            .Include(w => w.Product).ThenInclude(p => p.Brand)
            .Include(w => w.Product).ThenInclude(p => p.Category)
            .Where(w => w.UserId == UserId)
            .OrderByDescending(w => w.AddedAt)
            .ToListAsync();

        return View(items);
    }

    // POST /Wishlist/Toggle?productId=5
    // Toggle: nếu chưa có → thêm; đã có → xóa. Trả JSON cho AJAX.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int productId)
    {
        var existing = await db.WishlistItems
            .FirstOrDefaultAsync(w => w.UserId == UserId && w.ProductId == productId);

        bool added;
        if (existing != null)
        {
            db.WishlistItems.Remove(existing);
            added = false;
        }
        else
        {
            db.WishlistItems.Add(new WishlistItem
            {
                UserId = UserId,
                ProductId = productId,
                AddedAt = DateTime.UtcNow
            });
            added = true;
        }

        await db.SaveChangesAsync();

        int count = await db.WishlistItems.CountAsync(w => w.UserId == UserId);
        return Json(new { added, count, message = added ? "Đã thêm vào yêu thích!" : "Đã xóa khỏi yêu thích." });
    }

    // GET /Wishlist/Status?productId=5 — kiểm tra trạng thái (dùng khi render trang detail)
    [HttpGet]
    public async Task<IActionResult> Status(int productId)
    {
        bool liked = await db.WishlistItems
            .AnyAsync(w => w.UserId == UserId && w.ProductId == productId);
        return Json(new { liked });
    }

    // POST /Wishlist/Remove?id=10
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int id)
    {
        var item = await db.WishlistItems
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == UserId);
        if (item != null)
        {
            db.WishlistItems.Remove(item);
            await db.SaveChangesAsync();
        }
        return RedirectToAction("Index");
    }
}