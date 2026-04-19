namespace MilkStore.Models;

public class ChatMessage
{
    public int Id { get; set; }
    public string SessionId { get; set; } = "";
    public string Role { get; set; } = "";      // "user" | "bot" | "admin"
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? UserId { get; set; }            // null nếu khách chưa đăng nhập
    public bool IsRead { get; set; } = false;   // admin đã đọc chưa
}
