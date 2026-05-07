
// FILE: Models/Chatmessage.cs
// MỤC ĐÍCH: Entity model đại diện cho một tin nhắn trong hệ thống chat.
//           Map 1-1 với bảng "ChatMessages" trong PostgreSQL.
//
// CẤU TRÚC BẢNG (tạo tự động trong Program.cs):
//   Id        SERIAL PRIMARY KEY
//   SessionId VARCHAR(100) NOT NULL    -- phân nhóm theo phiên chat
//   Role      VARCHAR(20)  NOT NULL    -- "user" | "bot" | "admin"
//   Content   TEXT         NOT NULL
//   CreatedAt TIMESTAMPTZ  DEFAULT NOW()
//   UserId    INTEGER      NULL        -- null nếu khách vãng lai
//   IsRead    BOOLEAN      DEFAULT FALSE


namespace MilkStore.Models;


/// Đại diện cho một tin nhắn trong hệ thống chat của MilkStore.
/// Được dùng chung cho cả ba loại người gửi: khách hàng, bot AI, và admin.

public class ChatMessage
{
    Khóa chính — tự tăng (SERIAL trong PostgreSQL).
    public int Id { get; set; }

   
    /// ID phiên chat — chuỗi ngẫu nhiên do client tạo (thường là UUID hoặc timestamp).
    /// Dùng để nhóm tất cả tin nhắn trong cùng một cuộc trò chuyện.
    /// Ví dụ: "session_1714987200000"
   
    public string SessionId { get; set; } = "";

 
    /// Vai trò của người gửi. Có 3 giá trị hợp lệ:
    ///   "user"  — tin nhắn từ khách hàng
    ///   "bot"   — tin nhắn tự động từ AI (Groq/LLaMA)
    ///   "admin" — tin nhắn thủ công từ nhân viên hỗ trợ
   
    public string Role { get; set; } = "";      // "user" | "bot" | "admin"

    /// Nội dung tin nhắn — lưu toàn bộ text, không giới hạn độ dài (TEXT trong PostgreSQL).
    public string Content { get; set; } = "";

    
    /// Thời điểm tạo tin nhắn (UTC).
    /// Dùng UTC để nhất quán trên mọi timezone, convert sang giờ địa phương khi hiển thị.
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


    /// ID tài khoản của khách hàng (nếu đã đăng nhập).
    /// Nullable — null khi khách chưa đăng nhập (khách vãng lai).
    /// Không có khóa ngoại tường minh để tránh cascade delete xóa lịch sử chat.
  
    public int? UserId { get; set; }            // null nếu khách chưa đăng nhập

    
    /// Trạng thái đọc của admin.
    /// false = admin chưa đọc (hiển thị badge unread trên dashboard).
    /// true  = admin đã đọc, hoặc tin nhắn do bot/admin gửi (tự động mark = true).
   
    public bool IsRead { get; set; } = false;   // admin đã đọc chưa
}
