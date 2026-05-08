// ============================================================
// FILE: Controllers/AccountController.cs
// MỤC ĐÍCH: Xử lý toàn bộ luồng tài khoản người dùng.
//           Gồm: đăng nhập, đăng xuất, đăng ký, xem và cập nhật hồ sơ.
//
// CƠ CHẾ XÁC THỰC (Session-based):
//   Sau khi đăng nhập thành công, thông tin user được lưu vào Session:
//     Session["UserId"]   → dùng để xác định user trong mọi request
//     Session["RoleId"]   → dùng để phân quyền admin / khách
//     Session["FullName"] → dùng để hiển thị tên trên navbar
//     Session["Email"]    → thông tin hiển thị
//   Session tự hết hạn sau 2 giờ (cấu hình trong Program.cs).
//   Đăng xuất = Session.Clear() → tất cả thông tin bị xóa ngay lập tức.
//
// PHÂN QUYỀN SAU ĐĂNG NHẬP:
//   RoleId = 1 (Admin)    → redirect đến /Admin/Dashboard
//   RoleId = 2 (Khách)    → redirect đến trang chủ hoặc returnUrl
// ============================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkStore.Models;

namespace MilkStore.Controllers;

/// <summary>
/// Controller xử lý tài khoản: đăng nhập, đăng ký, đăng xuất, hồ sơ.
/// </summary>
public class AccountController(MilkStore4Context db) : Controller
{
    // --------------------------------------------------------
    // GET /Account/Login?returnUrl=/Cart/Index
    // Hiển thị trang đăng nhập.
    //
    // Nếu đã đăng nhập rồi → redirect về trang chủ (không cần login lại).
    // returnUrl: trang khách muốn vào trước khi bị chặn bởi [LoginRequired],
    //            lưu vào ViewBag để form POST gửi kèm theo.
    // --------------------------------------------------------
    /// <summary>Hiển thị trang đăng nhập. Nếu đã đăng nhập → về trang chủ.</summary>
    public IActionResult Login(string? returnUrl)
    {
        // Đã đăng nhập rồi → không cần login lại
        if (HttpContext.Session.GetInt32("UserId") != null)
            return RedirectToAction("Index", "Home");

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    // --------------------------------------------------------
    // POST /Account/Login
    // Xử lý đăng nhập: kiểm tra email + password trong DB.
    //
    // LUỒNG:
    //   1. Tìm user theo email VÀ password (cả hai phải khớp)
    //   2. Không tìm thấy → hiển thị lỗi, giữ nguyên trang
    //   3. Tìm thấy → lưu thông tin vào Session
    //   4. Redirect:
    //      - Nếu có returnUrl hợp lệ (local) → về trang đó
    //      - Admin (RoleId=1) → /Admin/Dashboard
    //      - Khách (RoleId=2) → trang chủ
    //
    // BẢO MẬT returnUrl:
    //   Url.IsLocalUrl() kiểm tra returnUrl là đường dẫn nội bộ
    //   → tránh Open Redirect Attack (kẻ tấn công redirect sang site giả mạo).
    // --------------------------------------------------------
    /// <summary>
    /// Xử lý đăng nhập. Xác thực email/password, lưu Session,
    /// rồi redirect theo role hoặc returnUrl.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl)
    {
        // Tìm user khớp cả email lẫn password (plain text so sánh trực tiếp)
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.Password == password);

        if (user == null)
        {
            // Sai email hoặc sai mật khẩu → hiển thị thông báo lỗi chung
            // (không nói rõ sai cái nào để tránh user enumeration)
            ViewBag.Error     = "Email hoặc mật khẩu không đúng.";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // Đăng nhập thành công → lưu thông tin vào Session
        HttpContext.Session.SetInt32("UserId",    user.Id);
        HttpContext.Session.SetInt32("RoleId",    user.RoleId);
        HttpContext.Session.SetString("FullName", user.FullName);
        HttpContext.Session.SetString("Email",    user.Email);

        // Ưu tiên returnUrl nếu hợp lệ và là local URL
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        // Phân luồng theo role
        return user.RoleId == 1
            ? RedirectToAction("Index", "Dashboard", new { area = "Admin" }) // Admin
            : RedirectToAction("Index", "Home");                              // Khách
    }

    // --------------------------------------------------------
    // GET /Account/Register
    // Hiển thị form đăng ký tài khoản mới.
    // Nếu đã đăng nhập → redirect về trang chủ.
    // --------------------------------------------------------
    /// <summary>Hiển thị trang đăng ký. Nếu đã đăng nhập → về trang chủ.</summary>
    public IActionResult Register()
    {
        if (HttpContext.Session.GetInt32("UserId") != null)
            return RedirectToAction("Index", "Home");
        return View();
    }

    // --------------------------------------------------------
    // POST /Account/Register
    // Tạo tài khoản mới.
    //
    // VALIDATION (2 bước trước khi lưu):
    //   1. password == confirmPassword → nếu không khớp → báo lỗi
    //   2. Email chưa tồn tại trong DB → nếu trùng → báo lỗi
    //
    // Tài khoản mới luôn có RoleId = 2 (khách hàng thông thường).
    // Sau khi đăng ký thành công → redirect về Login (không tự đăng nhập).
    // --------------------------------------------------------
    /// <summary>
    /// Đăng ký tài khoản mới. Kiểm tra mật khẩu khớp và email chưa tồn tại,
    /// rồi tạo user với RoleId = 2 (khách hàng).
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string fullName, string email,
        string password, string confirmPassword, string? address, string? phone)
    {
        // Bước 1: Kiểm tra mật khẩu nhập lại có khớp không
        if (password != confirmPassword)
        {
            ViewBag.Error = "Mật khẩu xác nhận không khớp.";
            return View();
        }

        // Bước 2: Kiểm tra email đã tồn tại chưa
        if (await db.Users.AnyAsync(u => u.Email == email))
        {
            ViewBag.Error = "Email này đã được đăng ký.";
            return View();
        }

        // Tạo tài khoản mới — RoleId = 2 luôn (không cho tự tạo admin)
        db.Users.Add(new User
        {
            RoleId   = 2,        // Khách hàng thông thường
            FullName = fullName,
            Email    = email,
            Password = password, // TODO: nên hash trước khi lưu
            Address  = address,
            Phone    = phone
        });
        await db.SaveChangesAsync();

        TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
        return RedirectToAction("Login"); // Yêu cầu đăng nhập thủ công
    }

    // --------------------------------------------------------
    // POST /Account/Logout
    // Đăng xuất: xóa toàn bộ Session → user bị coi là chưa đăng nhập.
    // Dùng POST (không phải GET) để tránh logout vô tình qua link.
    // --------------------------------------------------------
    /// <summary>Đăng xuất — xóa toàn bộ Session và redirect về trang Login.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        // Session.Clear() xóa tất cả key: UserId, RoleId, FullName, Email
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }

    // --------------------------------------------------------
    // GET /Account/Profile
    // Trang hồ sơ cá nhân — yêu cầu đăng nhập ([LoginRequired]).
    // Tải thông tin user từ DB theo UserId trong Session.
    // --------------------------------------------------------
    /// <summary>Trang hồ sơ cá nhân. Yêu cầu đăng nhập.</summary>
    [MilkStore.Filters.LoginRequired]
    public async Task<IActionResult> Profile()
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var user   = await db.Users.FindAsync(userId);
        return View(user);
    }

    // --------------------------------------------------------
    // POST /Account/UpdateProfile
    // Cập nhật thông tin hồ sơ: tên, địa chỉ, số điện thoại.
    // Email và mật khẩu KHÔNG được thay đổi ở đây (bảo mật).
    //
    // Sau khi lưu DB, cập nhật luôn Session["FullName"] để navbar
    // hiển thị tên mới ngay mà không cần đăng xuất/đăng nhập lại.
    // --------------------------------------------------------
    /// <summary>
    /// Cập nhật hồ sơ cá nhân (tên, địa chỉ, điện thoại).
    /// Đồng thời cập nhật Session để navbar hiển thị tên mới ngay.
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

            // Cập nhật Session ngay → navbar hiển thị tên mới tức thì
            // (không cần đăng xuất và đăng nhập lại)
            HttpContext.Session.SetString("FullName", fullName);

            TempData["Success"] = "Cập nhật tài khoản thành công!";
        }

        return RedirectToAction("Profile");
    }
}
