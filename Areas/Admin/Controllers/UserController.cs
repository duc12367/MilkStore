// ============================================================
// FILE: Areas/Admin/Controllers/UserController.cs
// MỤC ĐÍCH: Quản lý danh sách tài khoản người dùng từ phía admin.
//           Hiện tại chỉ có chức năng xem danh sách (read-only).
//
// BẢO MẬT:
//   [Area("Admin")] + [AdminOnly] → chỉ admin đăng nhập mới vào được.
// ============================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkStore.Filters;
using MilkStore.Models;

namespace MilkStore.Areas.Admin.Controllers;

/// <summary>
/// Controller quản lý tài khoản người dùng dành cho admin.
/// Hiện chỉ hỗ trợ xem danh sách — chưa có thêm/sửa/xóa.
/// </summary>
[Area("Admin")]
[AdminOnly]
public class UserController(MilkStore4Context db) : Controller
{
    // --------------------------------------------------------
    // GET /Admin/User/Index
    // Lấy toàn bộ danh sách user, Include Role để hiển thị
    // tên vai trò (Admin / Khách hàng) thay vì chỉ hiện số RoleId.
    // Sắp xếp: Admin (RoleId=1) lên trước, Khách (RoleId=2) xuống sau.
    // --------------------------------------------------------
    /// <summary>
    /// Danh sách tất cả tài khoản. Admin hiển thị trước, khách hàng sau.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var users = await db.Users
            .Include(u => u.Role)        // JOIN Roles → lấy tên vai trò
            .OrderBy(u => u.RoleId)      // Admin (1) lên trước, Khách (2) xuống sau
            .ToListAsync();

        return View(users);
    }
}
