
// FILE: Hubs/Chathub.cs
// MỤC ĐÍCH: SignalR Hub xử lý kết nối realtime cho tính năng chat.
//
// KIẾN TRÚC REALTIME:
//   - Khách hàng join vào group "session_{sessionId}"
//   - Admin join vào group "admins"
//   - Mỗi tin nhắn được broadcast đồng thời tới cả hai group
//     → Khách thấy bubble chat, Admin thấy trong dashboard
//
// CÁC ROLE TRONG HỆ THỐNG:
//   "user"  — tin nhắn từ khách hàng
//   "bot"   — tin nhắn tự động từ AI (Groq/LLaMA)
//   "admin" — tin nhắn thủ công từ nhân viên


using Microsoft.AspNetCore.SignalR;
using MilkStore.Models;

namespace MilkStore.Hubs;


/// SignalR Hub quản lý giao tiếp realtime giữa khách hàng và admin.
/// Hub này hỗ trợ 3 luồng tin nhắn: user → bot (AI), user → admin, admin → user.

public class ChatHub : Hub
{
    // Database context — dùng để lưu tin nhắn vào DB ngay khi nhận được
    private readonly MilkStore4Context _db;


    /// Constructor — inject DbContext qua DI.
    /// SignalR Hub được tạo mới mỗi khi có connection, nên DbContext cũng mới theo.

    public ChatHub(MilkStore4Context db)
    {
        _db = db;
    }

    // --------------------------------------------------------
    // Khách hàng gọi method này ngay khi mở cửa sổ chat.
    // Mỗi khách được join vào group riêng theo sessionId của mình,
    // đảm bảo tin nhắn chỉ đến đúng người nhận.
    //
    // Ví dụ: sessionId = "abc123" → group = "session_abc123"
    // --------------------------------------------------------
    
    /// Thêm connection hiện tại vào group của phiên chat.
    /// Gọi từ phía client (JavaScript) sau khi kết nối SignalR thành công.
   
    public async Task JoinSession(string sessionId)
    {
        // Context.ConnectionId là ID duy nhất của kết nối SignalR hiện tại
        await Groups.AddToGroupAsync(Context.ConnectionId, $"session_{sessionId}");
    }

    // --------------------------------------------------------
    // Admin gọi method này khi mở trang dashboard chat.
    // Tất cả admin dùng chung một group "admins" để nhận
    // mọi tin nhắn từ tất cả các session đang hoạt động.
    // --------------------------------------------------------
  
    /// Thêm connection của admin vào group "admins".
    /// Gọi từ trang admin dashboard khi admin đăng nhập vào hệ thống chat.
    
    public async Task JoinAdmin()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
    }

    // --------------------------------------------------------
    // Xử lý tin nhắn từ khách hàng gửi lên.
    // Flow:
    //   1. Tạo đối tượng ChatMessage và lưu vào DB
    //   2. Broadcast tới admin group (admin thấy realtime)
    //   3. Broadcast lại tới chính session của khách (hiển thị bubble)
    //
    // LƯU Ý: Trong thực tế, luồng gửi tin nhắn chủ yếu đi qua
    // ChatController.Send() (gọi API AI). Method này là fallback
    // hoặc dùng khi client muốn gửi trực tiếp qua WebSocket.
    // --------------------------------------------------------
    /// <summary>
    /// Nhận tin nhắn từ khách, lưu DB và broadcast tới admin và khách.
    /// </summary>
    /// <param name="sessionId">ID phiên chat của khách</param>
    /// <param name="content">Nội dung tin nhắn</param>
    /// <param name="userId">ID tài khoản (null nếu khách chưa đăng nhập)</param>
    public async Task SendUserMessage(string sessionId, string content, int? userId)
    {
        // Tạo entity tin nhắn mới — IsRead = false vì admin chưa đọc
        var msg = new ChatMessage
        {
            SessionId = sessionId,
            Role = "user",
            Content = content,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };
        _db.ChatMessages.Add(msg);
        await _db.SaveChangesAsync();

        // Broadcast tới admin để hiển thị trong dashboard realtime
        // Admin nhận được cả UserId để biết khách nào đang nhắn
        await Clients.Group("admins").SendAsync("ReceiveMessage", new
        {
            msg.Id,
            msg.SessionId,
            msg.Role,
            msg.Content,
            CreatedAt = msg.CreatedAt.ToString("HH:mm"),
            msg.UserId
        });

        // Broadcast lại cho chính khách — để hiển thị bubble tin nhắn đã gửi
        // (tránh khách phải tự thêm bubble ở client trước khi server confirm)
        await Clients.Group($"session_{sessionId}").SendAsync("ReceiveMessage", new
        {
            msg.Id,
            msg.SessionId,
            msg.Role,
            msg.Content,
            CreatedAt = msg.CreatedAt.ToString("HH:mm"),
            msg.UserId
        });
    }

    // --------------------------------------------------------
    // Gửi tin nhắn từ bot (AI) tới khách và admin.
    // Thường được gọi từ server-side sau khi nhận response từ Groq API.
    //
    // Khác với SendUserMessage:
    //   - IsRead = true (không cần đánh dấu unread cho bot)
    //   - Không có UserId
    // --------------------------------------------------------
    
    /// Broadcast tin nhắn của bot (AI) tới khách và admin sau khi AI phản hồi.
   
    /// <param name="sessionId">ID phiên chat cần nhận reply</param>
    /// <param name="content">Nội dung trả lời của AI</param>
    public async Task SendBotMessage(string sessionId, string content)
    {
        var msg = new ChatMessage
        {
            SessionId = sessionId,
            Role = "bot",
            Content = content,
            CreatedAt = DateTime.UtcNow,
            IsRead = true // Tin bot không cần đánh dấu "chưa đọc"
        };
        _db.ChatMessages.Add(msg);
        await _db.SaveChangesAsync();

        // Gửi tới khách để hiển thị bubble trả lời của bot
        await Clients.Group($"session_{sessionId}").SendAsync("ReceiveMessage", new
        {
            msg.Id,
            msg.SessionId,
            msg.Role,
            msg.Content,
            CreatedAt = msg.CreatedAt.ToString("HH:mm")
        });

        // Gửi tới admin để theo dõi cuộc hội thoại (admin xem bot nói gì)
        await Clients.Group("admins").SendAsync("ReceiveMessage", new
        {
            msg.Id,
            msg.SessionId,
            msg.Role,
            msg.Content,
            CreatedAt = msg.CreatedAt.ToString("HH:mm")
        });
    }

    // --------------------------------------------------------
    // Cho phép admin can thiệp thủ công vào cuộc hội thoại.
    // Ví dụ: bot trả lời sai, admin vào hỗ trợ trực tiếp.
    //
    // Tin nhắn admin được phân biệt với bot bởi role = "admin"
    // (trong UI thường hiển thị màu khác để khách nhận ra).
    // --------------------------------------------------------
   
    /// Admin gửi tin nhắn thủ công tới một phiên chat cụ thể.
    /// Dùng khi admin muốn can thiệp hỗ trợ trực tiếp thay bot.
    
    /// <param name="sessionId">ID phiên chat của khách cần hỗ trợ</param>
    /// <param name="content">Nội dung tin nhắn từ admin</param>
    public async Task SendAdminMessage(string sessionId, string content)
    {
        var msg = new ChatMessage
        {
            SessionId = sessionId,
            Role = "admin",
            Content = content,
            CreatedAt = DateTime.UtcNow,
            IsRead = true // Admin tự gửi nên coi như đã đọc
        };
        _db.ChatMessages.Add(msg);
        await _db.SaveChangesAsync();

        // Gửi tới khách hàng — đây là tin nhắn khách cần nhận
        await Clients.Group($"session_{sessionId}").SendAsync("ReceiveMessage", new
        {
            msg.Id,
            msg.SessionId,
            msg.Role,
            msg.Content,
            CreatedAt = msg.CreatedAt.ToString("HH:mm")
        });

        // Gửi lại tới dashboard admin — để các admin khác thấy được
        // (trường hợp nhiều admin cùng theo dõi một session)
        await Clients.Group("admins").SendAsync("ReceiveMessage", new
        {
            msg.Id,
            msg.SessionId,
            msg.Role,
            msg.Content,
            CreatedAt = msg.CreatedAt.ToString("HH:mm")
        });
    }
}
