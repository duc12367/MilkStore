// ============================================================
// FILE: Controllers/HomeController.cs
// MỤC ĐÍCH: Xử lý các trang tĩnh và trang chủ của MilkStore.
//           Gồm: trang chủ (hiển thị sản phẩm nổi bật),
//           trang lỗi, trang liên hệ, trang chính sách đổi trả.
//
// KHÔNG yêu cầu đăng nhập — khách vãng lai vẫn vào được.
// ============================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkStore.Models;

namespace MilkStore.Controllers;

/// <summary>
/// Controller xử lý trang chủ và các trang thông tin tĩnh.
/// Không yêu cầu đăng nhập.
/// </summary>
public class HomeController(MilkStore4Context db) : Controller
{
    // --------------------------------------------------------
    // GET /
    // Trang chủ — hiển thị 8 sản phẩm nổi bật ngẫu nhiên.
    //
    // LOGIC LẤY SẢN PHẨM:
    //   - Chỉ lấy sản phẩm còn hàng (StockQuantity > 0)
    //   - Sắp xếp ngẫu nhiên bằng OrderBy(_ => Guid.NewGuid())
    //     → mỗi lần tải trang, 8 sản phẩm hiển thị khác nhau
    //   - Include Brand và Category để lấy đủ thông tin
    //     trong một query duy nhất, không cần query thêm
    //
    // LƯU Ý: OrderBy(Guid.NewGuid()) ổn với dữ liệu nhỏ.
    // Nếu bảng Products lớn nên dùng EF.Functions.Random().
    // --------------------------------------------------------
    /// <summary>
    /// Trang chủ — lấy ngẫu nhiên 8 sản phẩm còn hàng để hiển thị nổi bật.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var featured = await db.Products
            .Include(p => p.Brand)              // JOIN Brands → lấy tên thương hiệu
            .Include(p => p.Category)           // JOIN Categories → lấy tên danh mục
            .Where(p => p.StockQuantity > 0)    // Chỉ hiển thị sản phẩm còn hàng
            .OrderBy(_ => Guid.NewGuid())       // Xáo ngẫu nhiên mỗi lần tải trang
            .Take(8)                            // Lấy tối đa 8 sản phẩm
            .ToListAsync();

        ViewBag.FeaturedProducts = featured;

        return View();
    }

    // --------------------------------------------------------
    // GET /error/{code}
    // Trang lỗi HTTP tập trung (404, 403, 500...).
    // Được middleware UseStatusCodePages trong Program.cs gọi
    // khi response trả về status code lỗi.
    //
    // Ví dụ: /error/404 → "Không tìm thấy trang"
    //        /error/500 → "Lỗi máy chủ"
    // code? nullable → nếu không truyền thì mặc định 500.
    // --------------------------------------------------------
    /// <summary>
    /// Trang lỗi tập trung. Nhận mã lỗi HTTP qua route, hiển thị thông báo phù hợp.
    /// </summary>
    [Route("error/{code?}")]
    public IActionResult Error(int? code)
    {
        ViewBag.Code = code ?? 500;
        return View();
    }

    // --------------------------------------------------------
    // GET /Home/Contact
    // Trang liên hệ — chỉ hiển thị thông tin tĩnh, không có logic.
    // --------------------------------------------------------
    /// <summary>Trang liên hệ — hiển thị thông tin liên hệ của MilkStore.</summary>
    public IActionResult Contact()
    {
        return View();
    }

    // --------------------------------------------------------
    // GET /Home/ReturnPolicy
    // Trang chính sách đổi trả — nội dung tĩnh, không cần DB.
    // --------------------------------------------------------
    /// <summary>Trang chính sách đổi trả hàng.</summary>
    public IActionResult ReturnPolicy()
    {
        return View();
    }
}
