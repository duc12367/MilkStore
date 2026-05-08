// ============================================================
// FILE: Models/Order.cs
// MỤC ĐÍCH: Model đại diện cho một đơn hàng đã đặt.
//           Map 1-1 với bảng "Orders" trong PostgreSQL.
//
// VÒNG ĐỜI TRẠNG THÁI (Status):
//   Pending  → đã đặt, chờ xác nhận / thanh toán
//   Paid     → đã thanh toán thành công (MoMo / VNPay xác nhận)
//   Shipping → đang giao hàng (admin cập nhật thủ công)
//   Cancelled→ đã hủy (khách hủy khi Pending, hoặc admin hủy)
//   Failed   → thanh toán online thất bại
// ============================================================

using System;
using System.Collections.Generic;

namespace MilkStore.Models;

/// <summary>
/// Đơn hàng của một khách, bao gồm toàn bộ thông tin giao dịch:
/// tổng tiền, địa chỉ giao, phương thức thanh toán, trạng thái.
/// </summary>
public partial class Order
{
    /// <summary>Khóa chính — tự tăng.</summary>
    public int Id { get; set; }

    /// <summary>ID tài khoản đặt hàng (khóa ngoại → bảng Users).</summary>
    public int UserId { get; set; }

    /// <summary>Thời điểm khách đặt hàng (lưu theo giờ server, không phải UTC).</summary>
    public DateTime OrderDate { get; set; }

    /// <summary>
    /// Tổng tiền được tính lúc PlaceOrder (= Σ giá × số lượng từng sản phẩm).
    /// Lưu vào đây để không bị ảnh hưởng nếu giá sản phẩm thay đổi sau này.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Trạng thái đơn hàng. Các giá trị hợp lệ:
    /// "Pending" | "Paid" | "Shipping" | "Cancelled" | "Failed"
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Phương thức thanh toán khách chọn lúc checkout.
    /// Các giá trị: "COD" (tiền mặt khi nhận) | "MoMo" | "VNPay"
    /// </summary>
    public string PaymentMethod { get; set; } = null!;

    /// <summary>Địa chỉ giao hàng do khách nhập, có thể khác địa chỉ tài khoản.</summary>
    public string? ShippingAddress { get; set; }

    /// <summary>Ghi chú thêm của khách (ví dụ: "giao buổi sáng", "gọi trước khi giao").</summary>
    public string? Note { get; set; }

    // Navigation properties
    /// <summary>
    /// Danh sách các dòng sản phẩm trong đơn hàng này.
    /// Nạp qua .Include(o => o.OrderItems).ThenInclude(oi => oi.Product).
    /// </summary>
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    /// <summary>Thông tin tài khoản chủ đơn hàng.</summary>
    public virtual User User { get; set; } = null!;
}
