using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkStore.Filters;
using MilkStore.Models;

namespace MilkStore.Controllers;

[LoginRequired]
public class CartController(MilkStore4Context db) : Controller
{
    private int UserId => HttpContext.Session.GetInt32("UserId")!.Value;

    // GET: /Cart
    public async Task<IActionResult> Index()
    {
        var items = await db.CartItems
            .Include(c => c.Product)
            .Where(c => c.UserId == UserId)
            .ToListAsync();

        decimal total = items.Sum(c => (c.Product?.Price ?? 0m) * c.Quantity);
        ViewBag.Total = total;

        return View(items);
    }

    // POST: /Cart/Add
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, int quantity = 1)
    {
        var item = await db.CartItems
            .FirstOrDefaultAsync(c => c.UserId == UserId && c.ProductId == productId);

        if (item == null)
        {
            db.CartItems.Add(new CartItem
            {
                UserId = UserId,
                ProductId = productId,
                Quantity = quantity
            });
        }
        else
        {
            item.Quantity += quantity;
        }

        await db.SaveChangesAsync();
        TempData["Success"] = "Đã thêm vào giỏ hàng!";
        return RedirectToAction("Index");
    }

    // POST: /Cart/Update
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int cartItemId, int quantity)
    {
        var item = await db.CartItems
            .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == UserId);

        if (item != null)
        {
            if (quantity <= 0)
                db.CartItems.Remove(item);
            else
                item.Quantity = quantity;

            await db.SaveChangesAsync();
        }

        return RedirectToAction("Index");
    }

    // POST: /Cart/Remove
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int cartItemId)
    {
        var item = await db.CartItems
            .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == UserId);

        if (item != null)
        {
            db.CartItems.Remove(item);
            await db.SaveChangesAsync();
        }

        return RedirectToAction("Index");
    }

    // POST: /Cart/Clear
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear()
    {
        var items = db.CartItems.Where(c => c.UserId == UserId);
        db.CartItems.RemoveRange(items);
        await db.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}
