using System;
using System.Collections.Generic;

namespace MilkStore.Models;

public partial class VwOrderDetail
{
    public int OrderId { get; set; }

    public DateTime OrderDate { get; set; }

    public string Status { get; set; } = null!;

    public string PaymentMethod { get; set; } = null!;

    public decimal TotalAmount { get; set; }

    public string? ShippingAddress { get; set; }

    public string CustomerName { get; set; } = null!;

    public string CustomerEmail { get; set; } = null!;

    public string? CustomerPhone { get; set; }

    public int Quantity { get; set; }

    public decimal PriceAtTime { get; set; }

    public decimal? LineTotal { get; set; }

    public string ProductName { get; set; } = null!;

    public string CategoryName { get; set; } = null!;

    public string BrandName { get; set; } = null!;
}
