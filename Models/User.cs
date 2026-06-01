// ============================================================
// FILE: Models/User.cs
// MỤC ĐÍCH: Model đại diện cho tài khoản người dùng.
//           Map 1-1 với bảng "Users" trong PostgreSQL.
//
// PHÂN QUYỀN (RoleId):
//   RoleId = 1 → Admin   (vào được khu vực /Admin)
//   RoleId = 2 → Khách hàng thông thường
//
// FIX TC10/TC11: Thêm field IsBlocked để Block/Unblock tài khoản
// ============================================================

using System;
using System.Collections.Generic;

namespace MilkStore.Models;

/// <summary>
/// Tài khoản người dùng của hệ thống MilkStore.
/// Dùng chung cho cả admin (RoleId=1) và khách hàng (RoleId=2).
/// </summary>
public partial class User
{
    /// <summary>Khóa chính — tự tăng.</summary>
    public int Id { get; set; }

    /// <summary>
    /// Vai trò của tài khoản (khóa ngoại → bảng Roles).
    ///   1 = Admin, 2 = Khách hàng.
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>Tên hiển thị. Được lưu vào Session sau khi đăng nhập.</summary>
    public string FullName { get; set; } = null!;

    /// <summary>Email — dùng làm tên đăng nhập, phải unique trong hệ thống.</summary>
    public string Email { get; set; } = null!;

    /// <summary>Mật khẩu (hash BCrypt).</summary>
    public string Password { get; set; } = null!;

    /// <summary>Địa chỉ giao hàng mặc định. Có thể null nếu chưa cập nhật.</summary>
    public string? Address { get; set; }

    /// <summary>Số điện thoại. Có thể null nếu chưa cập nhật.</summary>
    public string? Phone { get; set; }

    /// <summary>Số tài khoản ngân hàng (STK) dùng cho chuyển khoản.</summary>
    public string? BankAccountNumber { get; set; }

    /// <summary>Tên ngân hàng (VD: Vietcombank, Techcombank...).</summary>
    public string? BankName { get; set; }

    /// <summary>Mã OTP gần nhất được cấp (6 chữ số).</summary>
    public string? OtpCode { get; set; }

    /// <summary>Thời điểm cấp OTP (UTC). OTP hết hạn sau 5 phút.</summary>
    public DateTime? OtpIssuedAt { get; set; }

    /// <summary>Token dùng cho chức năng quên mật khẩu.</summary>
    public string? ResetToken { get; set; }

    /// <summary>Thời điểm hết hạn của ResetToken (UTC).</summary>
    public DateTime? ResetTokenExpiry { get; set; }

    /// <summary>
    /// FIX TC10/TC11: Tài khoản bị khóa hay không.
    /// true = bị khóa (không đăng nhập được), false = bình thường.
    /// </summary>
    public bool IsBlocked { get; set; } = false;

    /// <summary>
    /// Thời điểm tạo tài khoản (UTC).
    /// Dùng cho Dashboard thống kê user mới theo tháng.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation properties ────────────────────────────────
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    public virtual Role Role { get; set; } = null!;
}