// ============================================================
// FILE: Models/VwOrderDetail.cs
// MỤC ĐÍCH: Model ánh xạ từ Database View "VwOrderDetail".
//           View này JOIN nhiều bảng (Orders, OrderItems, Users,
//           Products, Categories, Brands) thành một bảng phẳng
//           để dễ hiển thị báo cáo chi tiết đơn hàng cho admin.
//
// LƯU Ý QUAN TRỌNG:
//   - Đây là READ-ONLY model — không dùng để Insert/Update/Delete.
//   - Mỗi dòng = 1 sản phẩm trong 1 đơn hàng (nếu đơn có 3 SP → 3 dòng).
//   - Không có khóa chính riêng vì đây là View, không phải Table.
// ============================================================

using System;
using System.Collections.Generic;

namespace MilkStore.Models;

/// <summary>
/// Kết quả truy vấn từ database view VwOrderDetail.
/// Dùng cho báo cáo admin — mỗi dòng chứa đủ thông tin
/// đơn hàng + khách hàng + sản phẩm mà không cần JOIN thêm.
/// </summary>
public partial class VwOrderDetail
{
    // ── THÔNG TIN ĐƠN HÀNG ──────────────────────────────────

    /// <summary>ID đơn hàng (từ bảng Orders).</summary>
    public int OrderId { get; set; }

    /// <summary>Ngày đặt hàng.</summary>
    public DateTime OrderDate { get; set; }

    /// <summary>Trạng thái đơn: Pending / Paid / Shipping / Cancelled / Failed.</summary>
    public string Status { get; set; } = null!;

    /// <summary>Phương thức thanh toán: COD / MoMo / VNPay.</summary>
    public string PaymentMethod { get; set; } = null!;

    /// <summary>Tổng tiền toàn đơn (đã tính sẵn lúc PlaceOrder).</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Địa chỉ giao hàng khách nhập.</summary>
    public string? ShippingAddress { get; set; }

    // ── THÔNG TIN KHÁCH HÀNG ────────────────────────────────

    /// <summary>Tên hiển thị của khách hàng.</summary>
    public string CustomerName { get; set; } = null!;

    /// <summary>Email tài khoản của khách hàng.</summary>
    public string CustomerEmail { get; set; } = null!;

    /// <summary>Số điện thoại (có thể null nếu khách chưa cập nhật).</summary>
    public string? CustomerPhone { get; set; }

    // ── THÔNG TIN TỪNG DÒNG SẢN PHẨM ───────────────────────

    /// <summary>Số lượng mua của sản phẩm trong dòng này.</summary>
    public int Quantity { get; set; }

    /// <summary>Giá tại thời điểm đặt hàng (snapshot, không đổi theo thời gian).</summary>
    public decimal PriceAtTime { get; set; }

    /// <summary>
    /// Tổng tiền dòng này = PriceAtTime × Quantity.
    /// Tính sẵn trong View bằng SQL (computed column).
    /// </summary>
    public decimal? LineTotal { get; set; }

    /// <summary>Tên sản phẩm (JOIN từ bảng Products).</summary>
    public string ProductName { get; set; } = null!;

    /// <summary>Tên danh mục sản phẩm (JOIN từ bảng Categories).</summary>
    public string CategoryName { get; set; } = null!;

    /// <summary>Tên thương hiệu sản phẩm (JOIN từ bảng Brands).</summary>
    public string BrandName { get; set; } = null!;
}
