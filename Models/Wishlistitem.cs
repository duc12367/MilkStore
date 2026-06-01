// FILE: Models/WishlistItem.cs
// Sản phẩm yêu thích của user — lưu vào DB, hiển thị ở trang Profile / Wishlist.

namespace MilkStore.Models;

public class WishlistItem
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ProductId { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public virtual User User { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}