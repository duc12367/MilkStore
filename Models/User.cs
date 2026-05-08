// ============================================================
// FILE: Models/User.cs
// MỤC ĐÍCH: Model đại diện cho tài khoản người dùng.
//           Map 1-1 với bảng "Users" trong PostgreSQL.
//
// PHÂN QUYỀN (RoleId):
//   RoleId = 1 → Admin   (vào được khu vực /Admin)
//   RoleId = 2 → Khách hàng thông thường
//
// LƯU Ý BẢO MẬT:
//   Password hiện lưu dạng plain text — cần nâng cấp lên
//   BCrypt hoặc PBKDF2 hash trước khi deploy production.
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
    /// Kiểm tra trong AccountController sau khi đăng nhập để redirect đúng trang.
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>Tên hiển thị. Được lưu vào Session sau khi đăng nhập.</summary>
    public string FullName { get; set; } = null!;

    /// <summary>Email — dùng làm tên đăng nhập, phải unique trong hệ thống.</summary>
    public string Email { get; set; } = null!;

    /// <summary>
    /// Mật khẩu. Hiện lưu plain text.
    /// TODO: nên hash bằng BCrypt trước khi lưu DB.
    /// </summary>
    public string Password { get; set; } = null!;

    /// <summary>Địa chỉ giao hàng mặc định. Có thể null nếu chưa cập nhật.</summary>
    public string? Address { get; set; }

    /// <summary>Số điện thoại. Có thể null nếu chưa cập nhật.</summary>
    public string? Phone { get; set; }

    // ── Navigation properties ────────────────────────────────
    // EF Core tự JOIN khi dùng .Include() — không cần query thêm.

    /// <summary>Danh sách sản phẩm trong giỏ hàng của user này.</summary>
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    /// <summary>Danh sách đơn hàng đã đặt của user này.</summary>
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    /// <summary>Danh sách đánh giá sản phẩm của user này.</summary>
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    /// <summary>Thông tin vai trò (tên role). Nạp qua .Include(u => u.Role).</summary>
    public virtual Role Role { get; set; } = null!;
}
