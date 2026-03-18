using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkStore.Filters;
using MilkStore.Models;

namespace MilkStore.Areas.Admin.Controllers;

[Area("Admin")]
[AdminOnly]
public class ProductController(MilkStore4Context db) : Controller
{
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        int pageSize = 10;
        var query = db.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(p => p.ProductName.Contains(search));

        int total = await query.CountAsync();
        var products = await query
            .OrderByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Search     = search;
        ViewBag.Page       = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
        return View(products);
    }

    public async Task<IActionResult> Create()
    {
        await LoadDropdowns();
        return View("Form", new Product());
    }

    public async Task<IActionResult> Edit(int id)
    {
        var product = await db.Products.FindAsync(id);
        if (product == null) return NotFound();
        await LoadDropdowns();
        return View("Form", product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(Product product, IFormFile? imageFile)
    {
        if (imageFile != null && imageFile.Length > 0)
        {
            var uploads = Path.Combine("wwwroot", "uploads");
            Directory.CreateDirectory(uploads);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
            using var stream = new FileStream(Path.Combine(uploads, fileName), FileMode.Create);
            await imageFile.CopyToAsync(stream);
            product.ImageUrl = $"/uploads/{fileName}";
        }

        if (product.Id == 0) db.Products.Add(product);
        else db.Products.Update(product);

        await db.SaveChangesAsync();
        TempData["Success"] = product.Id == 0 ? "Thêm sản phẩm thành công!" : "Cập nhật thành công!";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await db.Products.FindAsync(id);
        if (product != null) { db.Products.Remove(product); await db.SaveChangesAsync(); }
        TempData["Success"] = "Đã xóa sản phẩm.";
        return RedirectToAction("Index");
    }

    private async Task LoadDropdowns()
    {
        ViewBag.Categories = await db.Categories.ToListAsync();
        ViewBag.Brands     = await db.Brands.ToListAsync();
    }
}
