using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkStore.Models;

namespace MilkStore.Controllers;

public class ProductController(MilkStore4Context db) : Controller
{
    // GET: /Product
    public async Task<IActionResult> Index(int? categoryId, int? brandId,
        string? search, int page = 1)
    {
        int pageSize = 12;

        var query = db.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId);

        if (brandId.HasValue)
            query = query.Where(p => p.BrandId == brandId);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(p => p.ProductName.Contains(search));

        int total = await query.CountAsync();

        var products = await query
            .OrderBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Categories = await db.Categories.ToListAsync();
        ViewBag.Brands = await db.Brands.ToListAsync();
        ViewBag.CategoryId = categoryId;
        ViewBag.BrandId = brandId;
        ViewBag.Search = search;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);

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
            .Where(r => r.ProductId == id)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        ViewBag.Reviews = reviews;
        ViewBag.AvgRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

        return View(product);
    }

    // POST: /Product/AddReview
    [HttpPost]
    [ValidateAntiForgeryToken]
    [MilkStore.Filters.LoginRequired]
    public async Task<IActionResult> AddReview(int productId, int rating, string? comment)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;

        var already = await db.Reviews
            .AnyAsync(r => r.ProductId == productId && r.UserId == userId);

        if (!already)
        {
            db.Reviews.Add(new Review
            {
                ProductId = productId,
                UserId = userId,
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        return RedirectToAction("Detail", new { id = productId });
    }
}
