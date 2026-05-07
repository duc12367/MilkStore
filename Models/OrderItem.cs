
// FILE: Models/OrderItem.cs
// MỤC ĐÍCH: Model đại diện cho một dòng sản phẩm bên trong đơn hàng.
//           Map 1-1 với bảng "OrderItems" trong PostgreSQL.
//
// LÝ DO CÓ PriceAtTime:
//   Giá sản phẩm có thể thay đổi theo thời gian (admin cập nhật).
//   PriceAtTime lưu giá TẠI THỜI ĐIỂM đặt hàng → đảm bảo tổng tiền
//   đơn hàng không bị sai lệch về sau.
// ============================================================

using System;
using System.Collections.Generic;

namespace MilkStore.Models;

/// <summary>
/// Một dòng sản phẩm trong đơn hàng đã đặt.
/// Quan hệ: OrderItem thuộc về 1 Order và tham chiếu 1 Product.
/// </summary>
public partial class OrderItem
{
    /// <summary>Khóa chính — tự tăng.</summary>
    public int Id { get; set; }

    /// <summary>ID đơn hàng chứa dòng này (khóa ngoại → bảng Orders).</summary>
    public int OrderId { get; set; }

    /// <summary>ID sản phẩm được đặt (khóa ngoại → bảng Products).</summary>
    public int ProductId { get; set; }

    /// <summary>Số lượng đặt mua. Dùng để hoàn kho khi đơn bị hủy.</summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Giá của sản phẩm TẠI THỜI ĐIỂM đặt hàng (snapshot).
    /// Không dùng Product.Price vì giá có thể thay đổi sau này.
    /// Tổng dòng = PriceAtTime × Quantity.
    /// </summary>
    public decimal PriceAtTime { get; set; }

    // Navigation properties
    /// <summary>Đơn hàng cha chứa dòng này.</summary>
    public virtual Order Order { get; set; } = null!;

    /// <summary>Thông tin sản phẩm (tên, ảnh). Nạp qua ThenInclude(oi => oi.Product).</summary>
    public virtual Product Product { get; set; } = null!;
}
