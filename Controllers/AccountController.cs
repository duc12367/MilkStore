using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkStore.Models;

namespace MilkStore.Controllers;

public class AccountController : Controller
{
    private readonly MilkStore4Context db;
    private readonly MilkStore.Services.EmailService _email;

    public AccountController(MilkStore4Context db, MilkStore.Services.EmailService email)
    {
        this.db = db;
        _email = email;
    }

    public IActionResult Login(string? returnUrl)
    {
        if (HttpContext.Session.GetInt32("UserId") != null)
            return RedirectToAction("Index", "Home");
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.Password == password);

        if (user == null)
        {
            ViewBag.Error = "Email hoặc mật khẩu không đúng.";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetInt32("RoleId", user.RoleId);
        HttpContext.Session.SetString("FullName", user.FullName);
        HttpContext.Session.SetString("Email", user.Email);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return user.RoleId == 1
            ? RedirectToAction("Index", "Dashboard", new { area = "Admin" })
            : RedirectToAction("Index", "Home");
    }

    public IActionResult Register()
    {
        if (HttpContext.Session.GetInt32("UserId") != null)
            return RedirectToAction("Index", "Home");
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string fullName, string email,
        string password, string confirmPassword, string? address, string? phone)
    {
        if (password != confirmPassword)
        { ViewBag.Error = "Mật khẩu xác nhận không khớp."; return View(); }

        if (await db.Users.AnyAsync(u => u.Email == email))
        { ViewBag.Error = "Email này đã được đăng ký."; return View(); }

        db.Users.Add(new User
        {
            RoleId = 2,
            FullName = fullName,
            Email = email,
            Password = password,
            Address = address,
            Phone = phone
        });
        await db.SaveChangesAsync();
        TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
        return RedirectToAction("Login");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }

    [MilkStore.Filters.LoginRequired]
    public async Task<IActionResult> Profile()
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var user = await db.Users.FindAsync(userId);
        return View(user);
    }

    [MilkStore.Filters.LoginRequired]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(string fullName, string? address, string? phone)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var user = await db.Users.FindAsync(userId);
        if (user != null)
        {
            user.FullName = fullName;
            user.Address = address;
            user.Phone = phone;
            await db.SaveChangesAsync();
            HttpContext.Session.SetString("FullName", fullName);
            TempData["Success"] = "Cập nhật tài khoản thành công!";
        }
        return RedirectToAction("Profile");
    }

    // ==================== FORGOT / RESET PASSWORD ====================

    public IActionResult ForgotPassword() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            ViewBag.Msg = "Nếu email tồn tại, chúng mình đã gửi link đặt lại mật khẩu!";
            return View();
        }

        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                           .Replace("+", "").Replace("/", "").Replace("=", "");

        user.ResetToken = token;
        user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        await db.SaveChangesAsync();

        var resetLink = $"https://milkstore-2.onrender.com/Account/ResetPassword?token={token}";
        var body = $@"
        <div style='font-family:sans-serif;max-width:500px;margin:0 auto'>
            <h2 style='color:#1a3a2a'>🥛 MilkStore — Đặt lại mật khẩu</h2>
            <p>Xin chào <b>{user.FullName}</b>,</p>
            <p>Bạn vừa yêu cầu đặt lại mật khẩu. Nhấn nút bên dưới để tiếp tục:</p>
            <a href='{resetLink}' style='display:inline-block;padding:12px 28px;background:#1a3a2a;color:#fff;border-radius:8px;text-decoration:none;font-weight:700;margin:16px 0'>
                Đặt lại mật khẩu
            </a>
            <p style='color:#888;font-size:13px'>Link có hiệu lực trong <b>1 giờ</b>. Nếu bạn không yêu cầu, hãy bỏ qua email này.</p>
            <hr style='border:none;border-top:1px solid #eee;margin:20px 0'>
            <p style='color:#aaa;font-size:12px'>MilkStore — Sữa sạch cho cả gia đình 🥛</p>
        </div>";

        await _email.SendAsync(email, "Đặt lại mật khẩu MilkStore", body);
        ViewBag.Msg = " Đã gửi! Kiểm tra hộp thư của bạn (kể cả thư mục Spam).";
        return View();
    }

    public async Task<IActionResult> ResetPassword(string token)
    {
        var user = await db.Users.FirstOrDefaultAsync(u =>
            u.ResetToken == token && u.ResetTokenExpiry > DateTime.UtcNow);
        if (user == null)
        {
            ViewBag.Error = "Link đã hết hạn hoặc không hợp lệ!";
            return View();
        }
        ViewBag.Token = token;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string token, string newPassword)
    {
        var user = await db.Users.FirstOrDefaultAsync(u =>
            u.ResetToken == token && u.ResetTokenExpiry > DateTime.UtcNow);
        if (user == null)
        {
            ViewBag.Error = "Link đã hết hạn!";
            return View();
        }
        user.Password = newPassword;
        user.ResetToken = null;
        user.ResetTokenExpiry = null;
        await db.SaveChangesAsync();
        TempData["Success"] = "Đổi mật khẩu thành công! Hãy đăng nhập lại.";
        return RedirectToAction("Login");
    }
}