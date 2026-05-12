// ============================================================
// File: Controllers/AccountController.cs
// Chức năng: Xử lý đăng nhập, đăng ký, đăng xuất và hồ sơ
//            cá nhân của người dùng.
// Phụ thuộc: MilkStore4Context (EF Core), Session, Filters
// ============================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkStore.Models;

namespace MilkStore.Controllers;

/// <summary>
/// Controller quản lý tài khoản người dùng.
/// Dùng Primary Constructor để inject DbContext (C# 12).
/// </summary>
public class AccountController(MilkStore4Context db) : Controller
{
    // ─────────────────────────────────────────────
    // ĐĂNG NHẬP
    // ─────────────────────────────────────────────

    /// <summary>
    /// GET /Account/Login
    /// Hiển thị trang đăng nhập.
    /// Nếu đã đăng nhập → chuyển về trang chủ.
    /// </summary>
    /// <param name="returnUrl">URL cần quay lại sau khi đăng nhập thành công.</param>
    public IActionResult Login(string? returnUrl)
    {
        // Nếu session đã có UserId → người dùng đã đăng nhập, không cần vào lại
        if (HttpContext.Session.GetInt32("UserId") != null)
            return RedirectToAction("Index", "Home");

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    /// <summary>
    /// POST /Account/Login
    /// Xử lý đăng nhập: kiểm tra email + mật khẩu trong DB,
    /// lưu thông tin vào Session, rồi chuyển hướng phù hợp.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl)
    {
        // Tìm user khớp email VÀ mật khẩu (lưu ý: đang lưu plain-text, nên hash sau)
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.Password == password);

        if (user == null)
        {
            ViewBag.Error     = "Email hoặc mật khẩu không đúng.";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // Lưu thông tin người dùng vào Session sau khi đăng nhập thành công
        HttpContext.Session.SetInt32("UserId",  user.Id);
        HttpContext.Session.SetInt32("RoleId",  user.RoleId);
        HttpContext.Session.SetString("FullName", user.FullName);
        HttpContext.Session.SetString("Email",    user.Email);

        // Nếu có returnUrl hợp lệ (nội bộ), quay lại trang trước đó
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        // RoleId = 1 → Admin → vào dashboard; ngược lại → trang chủ user
        return user.RoleId == 1
            ? RedirectToAction("Index", "Dashboard", new { area = "Admin" })
            : RedirectToAction("Index", "Home");
    }

    // ─────────────────────────────────────────────
    // ĐĂNG KÝ
    // ─────────────────────────────────────────────

    /// <summary>
    /// GET /Account/Register
    /// Hiển thị form đăng ký.
    /// Nếu đã đăng nhập → chuyển về trang chủ.
    /// </summary>
    public IActionResult Register()
    {
        if (HttpContext.Session.GetInt32("UserId") != null)
            return RedirectToAction("Index", "Home");
        return View();
    }

    /// <summary>
    /// POST /Account/Register
    /// Tạo tài khoản mới với RoleId = 2 (người dùng thường).
    /// Kiểm tra mật khẩu khớp và email chưa tồn tại trước khi lưu.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string fullName, string email,
        string password, string confirmPassword, string? address, string? phone)
    {
        // Kiểm tra mật khẩu xác nhận
        if (password != confirmPassword)
        { ViewBag.Error = "Mật khẩu xác nhận không khớp."; return View(); }

        // Kiểm tra email đã được đăng ký chưa (tránh trùng lặp)
        if (await db.Users.AnyAsync(u => u.Email == email))
        { ViewBag.Error = "Email này đã được đăng ký."; return View(); }

        // Thêm user mới vào DB với role mặc định là 2 (khách hàng)
        db.Users.Add(new User
        {
            RoleId   = 2,
            FullName = fullName,
            Email    = email,
            Password = password,   // TODO: nên hash bằng BCrypt trước khi lưu
            Address  = address,
            Phone    = phone
        });
        await db.SaveChangesAsync();

        TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
        return RedirectToAction("Login");
    }

    // ─────────────────────────────────────────────
    // ĐĂNG XUẤT
    // ─────────────────────────────────────────────

    /// <summary>
    /// POST /Account/Logout
    /// Xóa toàn bộ Session → đăng xuất người dùng.
    /// Dùng POST để tránh CSRF (ai đó chèn link đăng xuất).
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear(); // Xóa hết dữ liệu session
        return RedirectToAction("Login");
    }

    // ─────────────────────────────────────────────
    // HỒ SƠ CÁ NHÂN
    // ─────────────────────────────────────────────

    /// <summary>
    /// GET /Account/Profile
    /// Hiển thị thông tin hồ sơ của người dùng đang đăng nhập.
    /// Yêu cầu đăng nhập (filter [LoginRequired]).
    /// </summary>
    [MilkStore.Filters.LoginRequired]
    public async Task<IActionResult> Profile()
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var user   = await db.Users.FindAsync(userId);
        return View(user);
    }

    /// <summary>
    /// POST /Account/UpdateProfile
    /// Cập nhật họ tên, địa chỉ, số điện thoại cho người dùng.
    /// Sau khi lưu, đồng bộ lại tên trong Session để navbar hiển thị đúng.
    /// </summary>
    [MilkStore.Filters.LoginRequired]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(string fullName, string? address, string? phone)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var user   = await db.Users.FindAsync(userId);

        if (user != null)
        {
            user.FullName = fullName;
            user.Address  = address;
            user.Phone    = phone;
            await db.SaveChangesAsync();

            // Cập nhật lại tên hiển thị trong session cho navbar
            HttpContext.Session.SetString("FullName", fullName);
            TempData["Success"] = "Cập nhật tài khoản thành công!";
        }

        return RedirectToAction("Profile");
    }
}
