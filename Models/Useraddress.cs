// FILE: Models/UserAddress.cs
// Lưu nhiều địa chỉ giao hàng cho 1 user.
// IsDefault = true → địa chỉ tự động điền vào Checkout.

namespace MilkStore.Models;

public class UserAddress
{
    public int Id { get; set; }
    public int UserId { get; set; }

    /// <summary>Tên hiển thị VD: "Nhà riêng", "Văn phòng"</summary>
    public string Label { get; set; } = "Địa chỉ";

    public string FullAddress { get; set; } = null!;
    public string? Phone { get; set; }
    public bool IsDefault { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual User User { get; set; } = null!;
}