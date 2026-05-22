// FILE: Areas/Admin/Controllers/CategoryController.cs
// MỤC ĐÍCH: Quản lý danh mục sản phẩm từ phía admin.
//           Gồm: xem danh sách (search + phân trang + sort), thêm,
//           sửa, xóa danh mục.
//
// CÁC TC ĐÃ XỬ LÝ:
//   [TC03]  Thêm trùng tên → báo lỗi, không lưu.
//   [TC04]  Tên rỗng / chỉ khoảng trắng → validate, báo lỗi.
//   [TC05]  Ký tự đặc biệt (@@@###) → validate regex, không lưu.
//   [TC07]  Sửa trùng tên danh mục khác → báo lỗi, không update.
//   [TC09]  Xóa danh mục có sản phẩm → chặn, báo lỗi rõ ràng.
//   [TC11]  Tìm kiếm không thấy → trả danh sách rỗng + thông báo.
//   [TC14]  Chưa login → [AdminOnly] chặn, redirect Login.
//   [TC17]  Mất DB → try-catch, toast lỗi thân thiện.
//
// BẢO MẬT:
//   [Area("Admin")] + [AdminOnly] → chỉ admin mới truy cập.

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkStore.Filters;
using MilkStore.Models;
using System.Text.RegularExpressions;

namespace MilkStore.Areas.Admin.Controllers;

[Area("Admin")]
[AdminOnly]
public class CategoryController(MilkStore4Context db) : Controller
{
    // ── Regex tên hợp lệ: chữ cái (kể cả tiếng Việt), số, khoảng trắng, dấu gạch ngang ──
    // Chặn các chuỗi toàn ký tự đặc biệt kiểu "@@@###", "!!!", "   " …
    private static readonly Regex ValidNameRegex =
        new(@"^[\p{L}\p{N}\s\-\.\/\(\)&]+$", RegexOptions.Compiled);

    // ============================================================
    // GET /Admin/Category/Index?search=...&sort=az&page=1
    //
    // [TC10] Search theo tên (case-insensitive, hỗ trợ tiếng Việt có dấu).
    // [TC11] Nếu search không tìm thấy → danh sách rỗng + ViewBag.NotFound.
    // [TC12] Phân trang 10 mục/trang.
    // [TC13] Sort A-Z / Z-A / mặc định (mới nhất trên).
    // [TC17] try-catch DB.
    // ============================================================
    public async Task<IActionResult> Index(string? search, string? sort, int page = 1)
    {
        const int pageSize = 10;

        // Sanitize search
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            if (search.Length > 100) search = search[..100];
            search = Regex.Replace(search, @"[\x00-\x1F\x7F]", "");
            if (string.IsNullOrWhiteSpace(search)) search = null;
        }

        try
        {
            var query = db.Categories
                .Include(c => c.Products)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(c => c.Name.ToLower().Contains(search.ToLower()));

            // [TC13] Sắp xếp
            query = sort switch
            {
                "az" => query.OrderBy(c => c.Name),
                "za" => query.OrderByDescending(c => c.Name),
                _ => query.OrderBy(c => c.Id)
            };

            int total = await query.CountAsync();

            // [TC11] Không tìm thấy kết quả
            if (total == 0 && !string.IsNullOrWhiteSpace(search))
                ViewBag.NotFound = $"Không tìm thấy danh mục nào khớp với \"{search}\".";

            var categories = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Sort = sort;
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.Total = total;

            return View(categories);
        }
        catch (Exception)
        {
            // [TC17] Mất DB
            TempData["Error"] = "Không thể kết nối cơ sở dữ liệu. Vui lòng thử lại sau.";
            return View(new List<Category>());
        }
    }

    // ============================================================
    // POST /Admin/Category/Create
    //
    // [TC03] Kiểm tra trùng tên (so sánh không phân biệt hoa thường + trim).
    // [TC04] Tên rỗng / khoảng trắng → trả lỗi.
    // [TC05] Ký tự đặc biệt → regex validation.
    // [TC17] try-catch DB.
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name)
    {
        // [TC04] Validate rỗng
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Tên danh mục không được để trống.";
            return RedirectToAction("Index");
        }

        name = name.Trim();

        // [TC05] Validate ký tự đặc biệt
        if (!ValidNameRegex.IsMatch(name))
        {
            TempData["Error"] = "Tên danh mục chứa ký tự không hợp lệ. Chỉ được dùng chữ cái, số và dấu cơ bản.";
            return RedirectToAction("Index");
        }

        if (name.Length > 100)
        {
            TempData["Error"] = "Tên danh mục tối đa 100 ký tự.";
            return RedirectToAction("Index");
        }

        try
        {
            // [TC03] Kiểm tra trùng tên
            bool exists = await db.Categories
                .AnyAsync(c => c.Name.ToLower() == name.ToLower());

            if (exists)
            {
                TempData["Error"] = $"Danh mục \"{name}\" đã tồn tại. Vui lòng chọn tên khác.";
                return RedirectToAction("Index");
            }

            db.Categories.Add(new Category { Name = name });
            await db.SaveChangesAsync();

            TempData["Success"] = $"Đã thêm danh mục \"{name}\" thành công.";
            return RedirectToAction("Index");
        }
        catch (Exception)
        {
            // [TC17]
            TempData["Error"] = "Lỗi hệ thống khi thêm danh mục. Vui lòng thử lại.";
            return RedirectToAction("Index");
        }
    }

    // ============================================================
    // POST /Admin/Category/Edit
    //
    // [TC07] Kiểm tra trùng tên với danh mục KHÁC (loại trừ chính nó).
    // [TC04] Validate rỗng.
    // [TC05] Validate ký tự đặc biệt.
    // [TC17] try-catch DB.
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string name)
    {
        // [TC04]
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Tên danh mục không được để trống.";
            return RedirectToAction("Index");
        }

        name = name.Trim();

        // [TC05]
        if (!ValidNameRegex.IsMatch(name))
        {
            TempData["Error"] = "Tên danh mục chứa ký tự không hợp lệ.";
            return RedirectToAction("Index");
        }

        try
        {
            var category = await db.Categories.FindAsync(id);
            if (category == null) return NotFound();

            // [TC07] Trùng tên với danh mục KHÁC (không phải chính nó)
            bool duplicate = await db.Categories
                .AnyAsync(c => c.Id != id && c.Name.ToLower() == name.ToLower());

            if (duplicate)
            {
                TempData["Error"] = $"Tên \"{name}\" đã được dùng bởi danh mục khác.";
                return RedirectToAction("Index");
            }

            category.Name = name;
            await db.SaveChangesAsync();

            TempData["Success"] = $"Đã cập nhật danh mục thành \"{name}\".";
            return RedirectToAction("Index");
        }
        catch (Exception)
        {
            // [TC17]
            TempData["Error"] = "Lỗi hệ thống khi cập nhật danh mục. Vui lòng thử lại.";
            return RedirectToAction("Index");
        }
    }

    // ============================================================
    // POST /Admin/Category/Delete
    //
    // [TC09] Không cho xóa danh mục đang có sản phẩm liên kết.
    //        Báo rõ số lượng sản phẩm đang dùng danh mục đó.
    // [TC17] try-catch DB.
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var category = await db.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return NotFound();

            // [TC09] Chặn xóa nếu còn sản phẩm
            if (category.Products.Count > 0)
            {
                TempData["Error"] =
                    $"Không thể xóa danh mục \"{category.Name}\": " +
                    $"đang có {category.Products.Count} sản phẩm thuộc danh mục này. " +
                    "Vui lòng chuyển hoặc xóa các sản phẩm trước.";
                return RedirectToAction("Index");
            }

            db.Categories.Remove(category);
            await db.SaveChangesAsync();

            TempData["Success"] = $"Đã xóa danh mục \"{category.Name}\".";
            return RedirectToAction("Index");
        }
        catch (Exception)
        {
            // [TC17]
            TempData["Error"] = "Lỗi hệ thống khi xóa danh mục. Vui lòng thử lại.";
            return RedirectToAction("Index");
        }
    }
}