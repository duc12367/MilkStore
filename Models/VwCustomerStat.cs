using System;
using System.Collections.Generic;

namespace MilkStore.Models;

public partial class VwCustomerStat
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public int? TotalOrders { get; set; }

    public decimal TotalSpent { get; set; }

    public DateTime? LastOrderDate { get; set; }
}
