using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkStore.Filters;
using MilkStore.Models;

namespace MilkStore.Areas.Admin.Controllers;

[Area("Admin")]
[AdminOnly]
public class ProductController(MilkStore4Context db) : Controller
{
    // ── Danh sách sản phẩm ──────────────────────────────────────────
    public async Task<IActionResult> Index(string? search, int? categoryId, int page = 1)
    {
        int pageSize = 10;
        var query = db.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            // [FIX TC13] Tìm không thấy → hiển thị rõ "không tìm thấy" thay vì danh sách trống
            query = query.Where(p =>
                p.ProductName.ToLower().Contains(s) ||
                (p.Brand != null && p.Brand.Name.ToLower().Contains(s)) ||
                (p.Description != null && p.Description.ToLower().Contains(s)));
        }

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId);

        int total = await query.CountAsync();
        var products = await query
            .OrderByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.CategoryId = categoryId;
        ViewBag.Categories = await db.Categories.ToListAsync();
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
        ViewBag.TotalFound = total;
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

    // ── Lưu sản phẩm (Thêm + Sửa) ──────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(Product product, IFormFile? imageFile)
    {
        // [FIX TC04] Tên không được để trống
        if (string.IsNullOrWhiteSpace(product.ProductName))
            ModelState.AddModelError("ProductName", "Vui lòng nhập tên sản phẩm.");

        // [FIX TC17] Tên không được chứa ký tự đặc biệt (chỉ cho phép chữ, số, khoảng trắng, dấu Việt)
        else if (System.Text.RegularExpressions.Regex.IsMatch(product.ProductName, @"[<>{}\[\]\\|*?!@#$%^&+=~`]"))
            ModelState.AddModelError("ProductName", "Tên sản phẩm không được chứa ký tự đặc biệt.");

        // [FIX TC03] Không cho trùng tên sản phẩm
        else if (await db.Products.AnyAsync(p =>
            p.ProductName.ToLower() == product.ProductName.Trim().ToLower() &&
            p.Id != product.Id))
            ModelState.AddModelError("ProductName", "Sản phẩm với tên này đã tồn tại.");

        // [FIX TC05] Giá không được âm hoặc bằng 0
        if (product.Price == null || product.Price <= 0)
            ModelState.AddModelError("Price", "Giá sản phẩm phải lớn hơn 0.");

        // [FIX TC06] Số lượng không được âm
        if (product.StockQuantity < 0)
            ModelState.AddModelError("StockQuantity", "Số lượng tồn kho không được âm.");

        // [FIX TC08] Kiểm tra định dạng file ảnh
        if (imageFile != null && imageFile.Length > 0)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var ext = Path.GetExtension(imageFile.FileName).ToLower();
            if (!allowed.Contains(ext))
                ModelState.AddModelError("imageFile", "Chỉ chấp nhận file ảnh (.jpg, .png, .gif, .webp).");
            else if (imageFile.Length > 5 * 1024 * 1024)
                ModelState.AddModelError("imageFile", "File ảnh không được vượt quá 5MB.");
        }

        if (!ModelState.IsValid)
        {
            await LoadDropdowns();
            return View("Form", product);
        }

        // Upload ảnh nếu hợp lệ
        if (imageFile != null && imageFile.Length > 0)
        {
            var uploads = Path.Combine("wwwroot", "uploads");
            Directory.CreateDirectory(uploads);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName).ToLower()}";
            using var stream = new FileStream(Path.Combine(uploads, fileName), FileMode.Create);
            await imageFile.CopyToAsync(stream);
            product.ImageUrl = $"/uploads/{fileName}";
        }

        product.ProductName = product.ProductName.Trim();

        if (product.Id == 0) db.Products.Add(product);
        else db.Products.Update(product);

        await db.SaveChangesAsync();
        TempData["Success"] = product.Id == 0 ? "Thêm sản phẩm thành công!" : "Cập nhật thành công!";
        return RedirectToAction("Index");
    }

    // ── Xóa sản phẩm ────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await db.Products
            .Include(p => p.OrderItems)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return NotFound();

        // [FIX TC11] Không cho xóa sản phẩm đang có trong đơn hàng
        if (product.OrderItems.Any())
        {
            TempData["Error"] = $"Không thể xóa '{product.ProductName}' vì đang có trong đơn hàng.";
            return RedirectToAction("Index");
        }

        db.Products.Remove(product);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã xóa sản phẩm.";
        return RedirectToAction("Index");
    }

    // ── AJAX: kiểm tra tên trùng real-time ─────────────────────────
    // [FIX TC03] Validate tên trùng ngay khi admin gõ
    [HttpGet]
    public async Task<IActionResult> CheckName(string name, int? excludeId)
    {
        name = name?.Trim() ?? "";
        var exists = await db.Products.AnyAsync(p =>
            p.ProductName.ToLower() == name.ToLower() &&
            p.Id != (excludeId ?? 0));
        return Json(new { exists });
    }

    private async Task LoadDropdowns()
    {
        ViewBag.Categories = await db.Categories.ToListAsync();
        ViewBag.Brands = await db.Brands.ToListAsync();
    }
}