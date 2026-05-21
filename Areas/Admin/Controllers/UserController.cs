// ============================================================
// FILE: Areas/Admin/Controllers/UserController.cs
// MỤC ĐÍCH: Quản lý tài khoản người dùng từ phía admin.
//


using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkStore.Filters;
using MilkStore.Models;

namespace MilkStore.Areas.Admin.Controllers;

[Area("Admin")]
[AdminOnly]
public class UserController(MilkStore4Context db) : Controller
{
    // --------------------------------------------------------
    // GET /Admin/User/Index?q=...
    // Tìm kiếm theo: tên, email, SĐT, hoặc mã khách hàng (U001...)
    // --------------------------------------------------------
    public async Task<IActionResult> Index(string? q)
    {
        ViewBag.Q = q;

        var query = db.Users
            .Include(u => u.Role)
            .Include(u => u.Orders)
            .OrderBy(u => u.RoleId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var lower = q.Trim().ToLower();

            // Tìm kiếm theo mã khách (VD: "U001", "u5", "U123")
            int? searchId = null;
            var codeMatch = Regex.Match(lower, @"^u(\d+)$");
            if (codeMatch.Success)
                searchId = int.TryParse(codeMatch.Groups[1].Value, out int cid) ? cid : null;

            query = query.Where(u =>
                (searchId.HasValue && u.Id == searchId.Value) ||
                u.FullName.ToLower().Contains(lower) ||
                u.Email.ToLower().Contains(lower) ||
                (u.Phone != null && u.Phone.Contains(lower))
            );
        }

        var users = await query.ToListAsync();
        return View(users);
    }

    // --------------------------------------------------------
    // POST /Admin/User/Add
    // FIX TC04, TC05, TC06, TC16
    // --------------------------------------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(string fullName, string email, string? phone, string? address, int roleId = 2)
    {
        // TC06: Validate tên không được trống
        if (string.IsNullOrWhiteSpace(fullName))
            return Json(new { success = false, message = "Tên không được để trống." });

        // TC16: Validate ký tự đặc biệt trong tên
        if (Regex.IsMatch(fullName, @"[<>""';&]"))
            return Json(new { success = false, message = "Tên chứa ký tự không hợp lệ." });

        // TC06: Validate email
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return Json(new { success = false, message = "Email không hợp lệ." });

        // TC05: Email không được trùng
        var exists = await db.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower());
        if (exists)
            return Json(new { success = false, message = "Email này đã được sử dụng." });

        var user = new User
        {
            FullName = fullName.Trim(),
            Email = email.Trim(),
            Password = BCrypt.Net.BCrypt.HashPassword("123456"), // mật khẩu mặc định
            Phone = phone,
            Address = address,
            RoleId = roleId,
            IsBlocked = false
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return Json(new { success = true, message = "Thêm khách hàng thành công.", userId = user.Id });
    }

    // --------------------------------------------------------
    // POST /Admin/User/Edit/{id}
    // FIX TC07: Sửa thông tin khách hàng
    // --------------------------------------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string fullName, string email, string? phone, string? address)
    {
        var user = await db.Users.FindAsync(id);
        if (user == null)
            return Json(new { success = false, message = "Không tìm thấy người dùng." });

        // TC06: Validate
        if (string.IsNullOrWhiteSpace(fullName))
            return Json(new { success = false, message = "Tên không được để trống." });

        if (Regex.IsMatch(fullName, @"[<>""';&]"))
            return Json(new { success = false, message = "Tên chứa ký tự không hợp lệ." });

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return Json(new { success = false, message = "Email không hợp lệ." });

        // TC05: Email không trùng với user khác
        var emailTaken = await db.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower() && u.Id != id);
        if (emailTaken)
            return Json(new { success = false, message = "Email này đã được sử dụng bởi tài khoản khác." });

        user.FullName = fullName.Trim();
        user.Email = email.Trim();
        user.Phone = phone;
        user.Address = address;

        await db.SaveChangesAsync();
        return Json(new { success = true, message = "Cập nhật thông tin thành công." });
    }

    // --------------------------------------------------------
    // POST /Admin/User/Delete/{id}
    // FIX TC08: Xóa bình thường
    // FIX TC09: Không xóa nếu có đơn hàng còn active (Pending/Shipping)
    // --------------------------------------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await db.Users
            .Include(u => u.Orders)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return Json(new { success = false, message = "Không tìm thấy người dùng." });

        // TC09: Không xóa nếu có đơn hàng đang mua
        var hasActiveOrders = user.Orders.Any(o =>
            o.Status == "Pending" || o.Status == "Shipping" || o.Status == "Paid");

        if (hasActiveOrders)
            return Json(new
            {
                success = false,
                message = "Không thể xóa khách hàng này vì họ đang có đơn hàng chưa hoàn tất. Hãy hoàn tất hoặc hủy đơn hàng trước."
            });

        db.Users.Remove(user);
        await db.SaveChangesAsync();
        return Json(new { success = true, message = "Đã xóa người dùng." });
    }

    // --------------------------------------------------------
    // POST /Admin/User/Block/{id}
    // FIX TC10: Khóa tài khoản
    // --------------------------------------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Block(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user == null)
            return Json(new { success = false, message = "Không tìm thấy người dùng." });

        user.IsBlocked = true;
        await db.SaveChangesAsync();
        return Json(new { success = true, message = $"Đã khóa tài khoản {user.FullName}." });
    }

    // --------------------------------------------------------
    // POST /Admin/User/Unblock/{id}
    // FIX TC11: Mở khóa tài khoản
    // --------------------------------------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unblock(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user == null)
            return Json(new { success = false, message = "Không tìm thấy người dùng." });

        user.IsBlocked = false;
        await db.SaveChangesAsync();
        return Json(new { success = true, message = $"Đã mở khóa tài khoản {user.FullName}." });
    }

    // --------------------------------------------------------
    // GET /Admin/User/GetUser/{id}
    // Trả JSON để populate form edit
    // --------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user == null) return NotFound();

        return Json(new
        {
            user.Id,
            user.FullName,
            user.Email,
            user.Phone,
            user.Address,
            user.RoleId,
            user.IsBlocked
        });
    }
}