using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkStore.Filters;
using MilkStore.Models;

namespace MilkStore.Areas.Admin.Controllers;

[Area("Admin")]
[AdminOnly]
public class ProductController(MilkStore4Context db) : Controller
{
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(
        int Id,
        string? ProductName,
        int CategoryId,
        int BrandId,
        decimal? Price,
        int StockQuantity,
        string? ExpiryDate,
        string? Description,
        string? ImageUrl,
        IFormFile? imageFile)
    {
        // Dùng tham số rời thay vì bind vào Product để tránh ModelState lỗi do auto-generated model
        var errors = new List<string>();

        // [FIX TC04] Tên bắt buộc
        if (string.IsNullOrWhiteSpace(ProductName))
            errors.Add("Vui lòng nhập tên sản phẩm.");
        // [FIX TC17] Không chứa ký tự đặc biệt
        else if (System.Text.RegularExpressions.Regex.IsMatch(ProductName,
            @"[<>{}\[\]\\|*?!@#$%^&+=~` ]".Replace(" ", "")))
            errors.Add("Tên sản phẩm không được chứa ký tự đặc biệt.");
        // [FIX TC03] Không trùng tên
        else if (await db.Products.AnyAsync(p =>
            p.ProductName.ToLower() == ProductName.Trim().ToLower() && p.Id != Id))
            errors.Add("Sản phẩm với tên này đã tồn tại.");

        // [FIX TC05] Giá > 0
        if (Price == null || Price <= 0)
            errors.Add("Giá sản phẩm phải lớn hơn 0.");

        // [FIX TC06] Số lượng >= 0
        if (StockQuantity < 0)
            errors.Add("Số lượng tồn kho không được âm.");

        // [FIX TC08] File ảnh đúng định dạng
        if (imageFile != null && imageFile.Length > 0)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var ext = Path.GetExtension(imageFile.FileName).ToLower();
            if (!allowed.Contains(ext))
                errors.Add("Chỉ chấp nhận file ảnh (.jpg, .png, .gif, .webp).");
            else if (imageFile.Length > 5 * 1024 * 1024)
                errors.Add("File ảnh không được vượt quá 5MB.");
        }

        if (errors.Any())
        {
            TempData["Error"] = string.Join(" | ", errors);
            await LoadDropdowns();
            // Dựng lại product để hiển thị lại form
            var p = new Product
            {
                Id = Id,
                ProductName = ProductName ?? "",
                CategoryId = CategoryId,
                BrandId = BrandId,
                Price = Price,
                StockQuantity = StockQuantity,
                Description = Description,
                ImageUrl = ImageUrl,
                ExpiryDate = DateOnly.TryParse(ExpiryDate, out var d) ? d : DateOnly.FromDateTime(DateTime.Today)
            };
            return View("Form", p);
        }

        // Upload ảnh
        string? newImageUrl = ImageUrl;
        if (imageFile != null && imageFile.Length > 0)
        {
            var uploads = Path.Combine("wwwroot", "uploads");
            Directory.CreateDirectory(uploads);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName).ToLower()}";
            using var stream = new FileStream(Path.Combine(uploads, fileName), FileMode.Create);
            await imageFile.CopyToAsync(stream);
            newImageUrl = $"/uploads/{fileName}";
        }

        if (Id == 0)
        {
            // Thêm mới
            var product = new Product
            {
                ProductName = ProductName!.Trim(),
                CategoryId = CategoryId,
                BrandId = BrandId,
                Price = Price,
                StockQuantity = StockQuantity,
                Description = Description,
                ImageUrl = newImageUrl,
                ExpiryDate = DateOnly.TryParse(ExpiryDate, out var d) ? d : DateOnly.FromDateTime(DateTime.Today)
            };
            db.Products.Add(product);
        }
        else
        {
            // Cập nhật
            var product = await db.Products.FindAsync(Id);
            if (product == null) return NotFound();
            product.ProductName = ProductName!.Trim();
            product.CategoryId = CategoryId;
            product.BrandId = BrandId;
            product.Price = Price;
            product.StockQuantity = StockQuantity;
            product.Description = Description;
            product.ImageUrl = newImageUrl;
            product.ExpiryDate = DateOnly.TryParse(ExpiryDate, out var d) ? d : product.ExpiryDate;
        }

        await db.SaveChangesAsync();
        TempData["Success"] = Id == 0 ? "Thêm sản phẩm thành công!" : "Cập nhật thành công!";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await db.Products
            .Include(p => p.OrderItems)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return NotFound();

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