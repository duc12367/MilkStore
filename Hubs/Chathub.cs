using Microsoft.AspNetCore.SignalR;
using MilkStore.Models;

namespace MilkStore.Hubs;

public class ChatHub : Hub
{
    private readonly MilkStore4Context _db;

    public ChatHub(MilkStore4Context db)
    {
        _db = db;
    }

    // Khách join vào group theo sessionId
    public async Task JoinSession(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"session_{sessionId}");
    }

    // Admin join vào group admin để nhận tất cả tin nhắn
    public async Task JoinAdmin()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
    }

    // Khách gửi tin nhắn → lưu DB → broadcast cho admin + gọi AI trả lời
    public async Task SendUserMessage(string sessionId, string content, int? userId)
    {
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

        // Gửi tới admin realtime
        await Clients.Group("admins").SendAsync("ReceiveMessage", new
        {
            msg.Id,
            msg.SessionId,
            msg.Role,
            msg.Content,
            CreatedAt = msg.CreatedAt.ToString("HH:mm"),
            msg.UserId
        });

        // Gửi lại cho chính khách (hiển thị bubble)
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

    // Bot trả lời (gọi từ server sau khi nhận response AI)
    public async Task SendBotMessage(string sessionId, string content)
    {
        var msg = new ChatMessage
        {
            SessionId = sessionId,
            Role = "bot",
            Content = content,
            CreatedAt = DateTime.UtcNow,
            IsRead = true
        };
        _db.ChatMessages.Add(msg);
        await _db.SaveChangesAsync();

        await Clients.Group($"session_{sessionId}").SendAsync("ReceiveMessage", new
        {
            msg.Id,
            msg.SessionId,
            msg.Role,
            msg.Content,
            CreatedAt = msg.CreatedAt.ToString("HH:mm")
        });

        await Clients.Group("admins").SendAsync("ReceiveMessage", new
        {
            msg.Id,
            msg.SessionId,
            msg.Role,
            msg.Content,
            CreatedAt = msg.CreatedAt.ToString("HH:mm")
        });
    }

    // Admin gửi tin nhắn thủ công
    public async Task SendAdminMessage(string sessionId, string content)
    {
        var msg = new ChatMessage
        {
            SessionId = sessionId,
            Role = "admin",
            Content = content,
            CreatedAt = DateTime.UtcNow,
            IsRead = true
        };
        _db.ChatMessages.Add(msg);
        await _db.SaveChangesAsync();

        // Gửi tới khách
        await Clients.Group($"session_{sessionId}").SendAsync("ReceiveMessage", new
        {
            msg.Id,
            msg.SessionId,
            msg.Role,
            msg.Content,
            CreatedAt = msg.CreatedAt.ToString("HH:mm")
        });

        // Gửi lại dashboard admin
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