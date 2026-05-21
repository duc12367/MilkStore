// FILE: Models/Coupon.cs
// Mã giảm giá — hỗ trợ giảm theo % hoặc số tiền cố định.

using System.ComponentModel.DataAnnotations;

namespace MilkStore.Models;

public class Coupon
{
    public int Id { get; set; }

    /// <summary>Mã nhập vào (VD: SALE10, SUMMER20). Lưu uppercase. Chỉ cho phép chữ cái và số.</summary>
    [Required(ErrorMessage = "Vui lòng nhập mã giảm giá.")]
    [RegularExpression(@"^[A-Z0-9]+$", ErrorMessage = "Mã giảm giá chỉ được chứa chữ cái và số.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Mã giảm giá phải từ 2 đến 50 ký tự.")]
    public string Code { get; set; } = null!;

    /// <summary>"Percent" = giảm %, "Fixed" = giảm tiền mặt (VNĐ).</summary>
    [Required]
    public string DiscountType { get; set; } = "Percent";

    /// <summary>Giá trị giảm: nếu Percent thì là % (0–100), nếu Fixed thì là VNĐ (> 0).</summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "Giá trị giảm phải lớn hơn 0.")]
    public decimal DiscountValue { get; set; }

    /// <summary>Ngày bắt đầu hiệu lực (UTC). Nếu > UtcNow → mã chưa có hiệu lực.</summary>
    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    /// <summary>Ngày hết hạn (UTC). Nếu < UtcNow → mã hết hạn.</summary>
    public DateTime ExpiryDate { get; set; }

    /// <summary>Số lần dùng tối đa (null = không giới hạn).</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Số lượt dùng phải ít nhất là 1.")]
    public int? MaxUsage { get; set; }

    /// <summary>Số lần đã dùng thực tế.</summary>
    public int UsageCount { get; set; } = 0;

    // ---- Computed helpers ----
    public bool IsActive(DateTime utcNow) =>
        utcNow >= StartDate &&
        utcNow <= ExpiryDate &&
        (MaxUsage == null || UsageCount < MaxUsage);
}