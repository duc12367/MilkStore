
using System;
using System.Collections.Generic;

namespace MilkStore.Models;

public partial class Review
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int ProductId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }

    //  MỚI: Để Admin có thể reply vào đánh giá
    public int? ParentReviewId { get; set; }

    public bool IsAdminReply { get; set; } = false;

    public virtual Product Product { get; set; } = null!;

    public virtual User User { get; set; } = null!;

    // Navigation: replies của review này
    public virtual ICollection<Review> Replies { get; set; } = new List<Review>();

    public virtual Review? ParentReview { get; set; }
}