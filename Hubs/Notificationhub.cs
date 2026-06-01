// FILE: Hubs/NotificationHub.cs
// SignalR hub cho thông báo realtime:
//   - Admin cập nhật trạng thái đơn hàng → khách nhận thông báo ngay
//   - Đơn mới → admin dashboard hiển thị badge + sound
//
// Mỗi user kết nối vào group "user_{userId}".
// Admin kết nối vào group "admin".

using Microsoft.AspNetCore.SignalR;

namespace MilkStore.Hubs;

public class NotificationHub : Hub
{
    // Khi client kết nối: tự join group theo userId từ query string
    public override async Task OnConnectedAsync()
    {
        var userId = Context.GetHttpContext()?.Session.GetInt32("UserId");
        var roleId = Context.GetHttpContext()?.Session.GetInt32("RoleId");

        if (userId.HasValue)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

            if (roleId == 1)  // Admin
                await Groups.AddToGroupAsync(Context.ConnectionId, "admin");
        }

        await base.OnConnectedAsync();
    }

    // Helper: server gọi để thông báo cho 1 user cụ thể
    public static async Task NotifyUser(IHubContext<NotificationHub> hub,
        int userId, string type, string message, string? url = null)
    {
        await hub.Clients.Group($"user_{userId}")
            .SendAsync("ReceiveNotification", new { type, message, url, time = DateTime.UtcNow });
    }

    // Helper: thông báo cho toàn bộ admin
    public static async Task NotifyAdmins(IHubContext<NotificationHub> hub,
        string type, string message, string? url = null)
    {
        await hub.Clients.Group("admin")
            .SendAsync("ReceiveNotification", new { type, message, url, time = DateTime.UtcNow });
    }
}