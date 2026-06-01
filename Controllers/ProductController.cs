// FILE: Controllers/ProductController.cs
//  Tìm kiếm nâng cao: lọc giá, sắp xếp, tìm theo tên/mô tả.

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkStore.Models;

namespace MilkStore.Controllers;

public class ProductController(MilkStore4Context db) : Controller
{
    // GET: /Product?categoryId=1&brandId=2&search=abc&minPrice=50000&maxPrice=500000&sort=price_asc&page=1
    public async Task<IActionResult> Index(
        int? categoryId, int? brandId,
        string? search,
        decimal? minPrice, decimal? maxPrice,
        string? sort,
        int page = 1)
    {
        int pageSize = 12;

        var query = db.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .AsQueryable();

        // ── Lọc ─────────────────────────────────────────────
        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId);

        if (brandId.HasValue)
            query = query.Where(p => p.BrandId == brandId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(p =>
                p.ProductName.ToLower().Contains(s) ||
                (p.Description != null && p.Description.ToLower().Contains(s)) ||
                (p.Brand != null && p.Brand.Name.ToLower().Contains(s)));
        }

        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice);

        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice);

        // ── Sắp xếp ─────────────────────────────────────────
        query = sort switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "name_asc" => query.OrderBy(p => p.ProductName),
            "name_desc" => query.OrderByDescending(p => p.ProductName),
            "newest" => query.OrderByDescending(p => p.Id),
            _ => query.OrderBy(p => p.Id)   // mặc định
        };

        int total = await query.CountAsync();

        var products = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Lấy giá min/max toàn bộ để render thanh lọc giá
        var priceRange = await db.Products
            .Where(p => p.Price.HasValue)
            .GroupBy(_ => 1)
            .Select(g => new { Min = g.Min(p => p.Price), Max = g.Max(p => p.Price) })
            .FirstOrDefaultAsync();

        ViewBag.Categories = await db.Categories.ToListAsync();
        ViewBag.Brands = await db.Brands.ToListAsync();
        ViewBag.CategoryId = categoryId;
        ViewBag.BrandId = brandId;
        ViewBag.Search = search;
        ViewBag.MinPrice = minPrice;
        ViewBag.MaxPrice = maxPrice;
        ViewBag.Sort = sort;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
        ViewBag.TotalFound = total;
        ViewBag.PriceMin = priceRange?.Min ?? 0;
        ViewBag.PriceMax = priceRange?.Max ?? 10_000_000;

        return View(products);
    }

    // GET: /Product/Detail/5
    public async Task<IActionResult> Detail(int id)
    {
        var product = await db.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return NotFound();

        var reviews = await db.Reviews
            .Include(r => r.User)
            .Include(r => r.Replies).ThenInclude(rep => rep.User)
            .Where(r => r.ProductId == id && r.ParentReviewId == null)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        // Sản phẩm cùng danh mục (gợi ý)
        var related = await db.Products
            .Include(p => p.Brand)
            .Where(p => p.CategoryId == product.CategoryId && p.Id != id && p.StockQuantity > 0)
            .OrderBy(_ => Guid.NewGuid())
            .Take(4)
            .ToListAsync();

        ViewBag.Reviews = reviews;
        ViewBag.AvgRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
        ViewBag.Related = related;

        // Wishlist status (nếu đã đăng nhập)
        var userId = HttpContext.Session.GetInt32("UserId");
        ViewBag.IsWishlisted = userId.HasValue &&
            await db.WishlistItems.AnyAsync(w => w.UserId == userId && w.ProductId == id);

        return View(product);
    }

    // POST: /Product/AddReview
    [HttpPost]
    [ValidateAntiForgeryToken]
    [MilkStore.Filters.LoginRequired]
    public async Task<IActionResult> AddReview(int productId, int rating, string? comment)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;

        // Chỉ cho review nếu đã mua sản phẩm này
        bool purchased = await db.OrderItems
            .AnyAsync(oi => oi.ProductId == productId &&
                            oi.Order.UserId == userId &&
                            (oi.Order.Status == "Paid" || oi.Order.Status == "Shipping"));

        if (!purchased)
        {
            TempData["Error"] = "Bạn cần mua sản phẩm này trước khi đánh giá.";
            return RedirectToAction("Detail", new { id = productId });
        }

        bool already = await db.Reviews
            .AnyAsync(r => r.ProductId == productId && r.UserId == userId && r.ParentReviewId == null);

        if (!already)
        {
            db.Reviews.Add(new Review
            {
                ProductId = productId,
                UserId = userId,
                Rating = Math.Clamp(rating, 1, 5),
                Comment = comment?.Trim(),
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            TempData["Success"] = "Cảm ơn bạn đã đánh giá!";
        }
        else
        {
            TempData["Error"] = "Bạn đã đánh giá sản phẩm này rồi.";
        }

        return RedirectToAction("Detail", new { id = productId });
    }

    // GET: /Product/Search?q=vinamilk — API tìm kiếm nhanh (autocomplete)
    [HttpGet]
    public async Task<IActionResult> Search(string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Json(new List<object>());

        var results = await db.Products
            .Where(p => p.ProductName.ToLower().Contains(q.ToLower()) && p.StockQuantity > 0)
            .OrderBy(p => p.ProductName)
            .Take(8)
            .Select(p => new
            {
                p.Id,
                p.ProductName,
                p.ImageUrl,
                Price = p.Price ?? 0
            })
            .ToListAsync();

        return Json(results);
    }
}