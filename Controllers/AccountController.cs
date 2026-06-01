using BCrypt.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkStore.Filters;
using MilkStore.Models;
using Org.BouncyCastle.Crypto.Generators;

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

    [RateLimit(maxRequests: 10, windowSeconds: 60)]  // max 10 lần đăng nhập / 60 giây
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);

        // ← BCRYPT: verify mật khẩu, hỗ trợ cả plain text cũ lẫn hash mới
        bool valid = false;
        if (user != null)
        {
            if (user.Password.StartsWith("$2"))
            {
                // Mật khẩu đã được hash BCrypt
                valid = BCrypt.Net.BCrypt.Verify(password, user.Password);
            }
            else
            {
                // Mật khẩu cũ plain text → đăng nhập được, tự động hash lại
                valid = user.Password == password;
                if (valid)
                {
                    user.Password = BCrypt.Net.BCrypt.HashPassword(password);
                    await db.SaveChangesAsync();
                }
            }
        }

        if (!valid || user == null)
        {
            ViewBag.Error = "Email hoặc mật khẩu không đúng.";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // FIX TC10: Kiểm tra tài khoản bị khóa — không cho đăng nhập
        if (user.IsBlocked)
        {
            ViewBag.Error = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.";
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

    [RateLimit(maxRequests: 5, windowSeconds: 300)]  // max 5 lần đăng ký / 5 phút
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string fullName, string email,
        string password, string confirmPassword, string? address, string? phone)
    {
        if (password != confirmPassword)
        { ViewBag.Error = "Mật khẩu xác nhận không khớp."; return View(); }

        if (password.Length < 8)
        { ViewBag.Error = "Mật khẩu phải có ít nhất 8 ký tự."; return View(); }

        if (await db.Users.AnyAsync(u => u.Email == email))
        { ViewBag.Error = "Email này đã được đăng ký."; return View(); }

        db.Users.Add(new User
        {
            RoleId = 2,
            FullName = fullName,
            Email = email,
            Password = BCrypt.Net.BCrypt.HashPassword(password), // ← HASH mật khẩu
            Address = address,
            Phone = phone,
            IsBlocked = false
        });
        await db.SaveChangesAsync();
        TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
        return RedirectToAction("Login");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync("Cookies");
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

    [RateLimit(maxRequests: 3, windowSeconds: 300)]  // max 3 lần gửi email reset / 5 phút
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
            <p style='color:#888;font-size:13px'>Link có hiệu lực trong <b>1 giờ</b>.</p>
        </div>";

        await _email.SendAsync(email, "Đặt lại mật khẩu MilkStore", body);
        ViewBag.Msg = "Đã gửi! Kiểm tra hộp thư của bạn (kể cả thư mục Spam).";
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
        if (newPassword.Length < 8)
        {
            ViewBag.Error = "Mật khẩu phải có ít nhất 8 ký tự.";
            ViewBag.Token = token;
            return View();
        }

        var user = await db.Users.FirstOrDefaultAsync(u =>
            u.ResetToken == token && u.ResetTokenExpiry > DateTime.UtcNow);
        if (user == null)
        {
            ViewBag.Error = "Link đã hết hạn!";
            return View();
        }

        user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword); // ← HASH mật khẩu mới
        user.ResetToken = null;
        user.ResetTokenExpiry = null;
        await db.SaveChangesAsync();
        TempData["Success"] = "Đổi mật khẩu thành công! Hãy đăng nhập lại.";
        return RedirectToAction("Login");
    }

    // ============================================================
    // FACEBOOK - XÓA DỮ LIỆU
    // ============================================================
    [HttpGet]
    public IActionResult DeleteData()
    {
        return Content(
            "Để yêu cầu xóa dữ liệu cá nhân, vui lòng liên hệ: vanduczai1@gmail.com",
            "text/plain"
        );
    }

    // ============================================================
    // GOOGLE OAUTH
    // ============================================================
    public IActionResult GoogleLogin(string? returnUrl = "/")
    {
        var props = new AuthenticationProperties
        {
            RedirectUri = Url.Action("GoogleCallback", "Account", new { returnUrl })
        };
        props.Parameters.Add("prompt", "select_account");
        return Challenge(props, "Google");
    }

    public async Task<IActionResult> GoogleCallback(string? returnUrl = "/")
    {
        var result = await HttpContext.AuthenticateAsync("Cookies");
        if (!result.Succeeded) return RedirectToAction("Login");

        var claims = result.Principal?.Claims;
        var email = claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value ?? "";
        var fullName = claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Name)?.Value ?? "Google User";

        if (string.IsNullOrEmpty(email)) return RedirectToAction("Login");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            // Google/Facebook user không cần password thật → lưu hash của GUID random
            user = new User
            {
                Email = email,
                FullName = fullName,
                Password = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                RoleId = 2,
                IsBlocked = false
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        // FIX TC10: Chặn Google login nếu tài khoản bị khóa
        if (user.IsBlocked)
        {
            ViewBag.Error = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.";
            return View("Login");
        }

        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetInt32("RoleId", user.RoleId);
        HttpContext.Session.SetString("FullName", user.FullName ?? "");
        HttpContext.Session.SetString("Email", user.Email ?? "");
        HttpContext.Session.SetString("Role", user.RoleId == 1 ? "Admin" : "Customer");

        await HttpContext.SignOutAsync("Cookies");
        return Redirect(returnUrl ?? "/");
    }

    // ============================================================
    // FACEBOOK OAUTH
    // ============================================================
    public IActionResult FacebookLogin(string? returnUrl = "/")
    {
        var props = new AuthenticationProperties
        {
            RedirectUri = Url.Action("FacebookCallback", "Account", new { returnUrl })
        };
        return Challenge(props, "Facebook");
    }

    public async Task<IActionResult> FacebookCallback(string? returnUrl = "/")
    {
        var result = await HttpContext.AuthenticateAsync("Cookies");
        if (!result.Succeeded) return RedirectToAction("Login");

        var claims = result.Principal?.Claims;
        var email = claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value ?? "";
        var fullName = claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Name)?.Value ?? "Facebook User";

        if (string.IsNullOrEmpty(email))
        {
            var fbId = claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
            email = $"fb_{fbId}@facebook.com";
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            user = new User
            {
                Email = email,
                FullName = fullName,
                Password = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                RoleId = 2,
                IsBlocked = false
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        // FIX TC10: Chặn Facebook login nếu tài khoản bị khóa
        if (user.IsBlocked)
        {
            ViewBag.Error = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.";
            return View("Login");
        }

        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetInt32("RoleId", user.RoleId);
        HttpContext.Session.SetString("FullName", user.FullName ?? "");
        HttpContext.Session.SetString("Email", user.Email ?? "");
        HttpContext.Session.SetString("Role", user.RoleId == 1 ? "Admin" : "Customer");

        await HttpContext.SignOutAsync("Cookies");
        return Redirect(returnUrl ?? "/");
    }
}