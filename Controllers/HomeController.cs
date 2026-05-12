// ============================================================
// File: Controllers/HomeController.cs
// Chức năng: Xử lý trang chủ, trang liên hệ và chính sách
//            đổi trả của MilkStore.
// Dữ liệu truyền view: FeaturedProducts, Categories, Brands
// ============================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkStore.Models;

namespace MilkStore.Controllers;

/// <summary>
/// Controller trang chủ và các trang tĩnh (liên hệ, chính sách).
/// </summary>
public class HomeController(MilkStore4Context db) : Controller
{
    /// <summary>
    /// GET /
    /// Trang chủ: hiển thị banner hero, sản phẩm nổi bật,
    /// danh mục và thương hiệu.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        // Lấy tối đa 8 sản phẩm nổi bật để hiển thị ở section đầu trang
        var featured = await db.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Where(p => p.IsFeatured == true)
            .Take(8)
            .ToListAsync();

        // Danh mục & thương hiệu dùng cho navigation/filter trên trang chủ
        ViewBag.FeaturedProducts = featured;
        ViewBag.Categories       = await db.Categories.ToListAsync();
        ViewBag.Brands           = await db.Brands.ToListAsync();

        return View();
    }

    /// <summary>
    /// GET /Home/Contact
    /// Trang liên hệ — hiển thị thông tin liên lạc và form gửi tin.
    /// </summary>
    public IActionResult Contact() => View();

    /// <summary>
    /// GET /Home/ReturnPolicy
    /// Trang chính sách đổi trả — nội dung tĩnh.
    /// </summary>
    public IActionResult ReturnPolicy() => View();
}
