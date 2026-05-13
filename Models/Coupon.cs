// FILE: Models/Coupon.cs
// Mã giảm giá — hỗ trợ giảm theo % hoặc số tiền cố định.

namespace MilkStore.Models;

public class Coupon
{
    public int Id { get; set; }

    /// <summary>Mã nhập vào (VD: SALE10, SUMMER20). Lưu uppercase.</summary>
    public string Code { get; set; } = null!;

    /// <summary>"Percent" = giảm %, "Fixed" = giảm tiền mặt (VNĐ).</summary>
    public string DiscountType { get; set; } = "Percent";

    /// <summary>Giá trị giảm: nếu Percent thì là %, nếu Fixed thì là VNĐ.</summary>
    public decimal DiscountValue { get; set; }

    /// <summary>Ngày hết hạn (UTC). Nếu < UtcNow → mã hết hạn.</summary>
    public DateTime ExpiryDate { get; set; }
}