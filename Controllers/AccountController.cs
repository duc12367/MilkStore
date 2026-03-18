using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkStore.Models;

namespace MilkStore.Controllers;

public class AccountController(MilkStore4Context db) : Controller
{
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
            ViewBag.Error     = "Email hoặc mật khẩu không đúng.";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        HttpContext.Session.SetInt32("UserId",  user.Id);
        HttpContext.Session.SetInt32("RoleId",  user.RoleId);
        HttpContext.Session.SetString("FullName", user.FullName);
        HttpContext.Session.SetString("Email",    user.Email);

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
            RoleId = 2, FullName = fullName, Email = email,
            Password = password, Address = address, Phone = phone
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
        var user   = await db.Users.FindAsync(userId);
        return View(user);
    }

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
            HttpContext.Session.SetString("FullName", fullName);
            TempData["Success"] = "Cập nhật tài khoản thành công!";
        }
        return RedirectToAction("Profile");
    }
}
