using System;
using System.Collections.Generic;

namespace MilkStore.Models;

public partial class VwProductStat
{
    public int Id { get; set; }

    public string ProductName { get; set; } = null!;

    public string Category { get; set; } = null!;

    public string Brand { get; set; } = null!;

    public decimal? Price { get; set; }

    public int StockQuantity { get; set; }

    public int TotalSold { get; set; }

    public decimal TotalRevenue { get; set; }

    public decimal AvgRating { get; set; }

    public int? ReviewCount { get; set; }
}
