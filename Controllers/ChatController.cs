using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MilkStore.Hubs;
using MilkStore.Models;
using System.Text;
using System.Text.Json;

namespace MilkStore.Controllers;

public class ChatController : Controller
{
    private readonly MilkStore4Context _db;
    private readonly IHubContext<ChatHub> _hub;
    private readonly IHttpClientFactory _http;
    private const string GROQ_API = "https://api.groq.com/openai/v1/chat/completions";

    private static readonly string SYSTEM_PROMPT = @"
Bạn là Mai — chuyên gia tư vấn sữa của MilkStore.
- Tư vấn sản phẩm sữa phù hợp (bé sơ sinh, trẻ em, mẹ bầu, người lớn, tiểu đường...)
- Hỏi thêm nếu chưa đủ thông tin (tuổi, nhu cầu, ngân sách)
- Xưng 'mình' - 'bạn', thêm emoji nhẹ
- Trả lời ngắn gọn, thân thiện
- Luôn hỏi cuối: 'Bạn có muốn mình đặt hàng luôn không ạ?'";

    public ChatController(MilkStore4Context db, IHubContext<ChatHub> hub, IHttpClientFactory http)
    {
        _db = db;
        _hub = hub;
        _http = http;
    }

    // GET /Chat/History?sessionId=xxx — lấy lịch sử chat
    [HttpGet]
    public async Task<IActionResult> History(string sessionId)
    {
        var msgs = await _db.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new { m.Role, m.Content, CreatedAt = m.CreatedAt.ToString("HH:mm") })
            .ToListAsync();
        return Json(msgs);
    }

    // POST /Chat/Send — nhận tin từ khách, gọi AI, trả về
    [HttpPost]
    public async Task<IActionResult> Send([FromBody] ChatSendDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.SessionId) || string.IsNullOrWhiteSpace(dto.Message))
            return BadRequest();

        var userId = HttpContext.Session.GetInt32("UserId");

        // 1. Lưu tin nhắn của khách
        await _hub.Clients.Group($"session_{dto.SessionId}").SendAsync("ReceiveMessage", new
        {
            SessionId = dto.SessionId,
            Role = "user",
            Content = dto.Message,
            CreatedAt = DateTime.UtcNow.ToString("HH:mm")
        });
        await _hub.Clients.Group("admins").SendAsync("ReceiveMessage", new
        {
            SessionId = dto.SessionId,
            Role = "user",
            Content = dto.Message,
            CreatedAt = DateTime.UtcNow.ToString("HH:mm"),
            UserId = userId
        });

        _db.ChatMessages.Add(new ChatMessage
        {
            SessionId = dto.SessionId,
            Role = "user",
            Content = dto.Message,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        // 2. Lấy lịch sử để gửi context cho AI
        var history = await _db.ChatMessages
            .Where(m => m.SessionId == dto.SessionId && (m.Role == "user" || m.Role == "bot"))
            .OrderBy(m => m.CreatedAt)
            .TakeLast(10)
            .ToListAsync();

        var messages = new List<object> { new { role = "system", content = SYSTEM_PROMPT } };
        foreach (var h in history)
            messages.Add(new { role = h.Role == "bot" ? "assistant" : "user", content = h.Content });

        // 3. Gọi Groq AI
        string reply;
        try
        {
            var groqKey = Environment.GetEnvironmentVariable("GROQ_API_KEY")
                          ?? throw new Exception("GROQ_API_KEY not set");

            var client = _http.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {groqKey}");

            var body = JsonSerializer.Serialize(new
            {
                model = "llama-3.3-70b-versatile",
                messages,
                max_tokens = 400
            });

            var res = await client.PostAsync(GROQ_API, new StringContent(body, Encoding.UTF8, "application/json"));
            var json = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            reply = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "Xin lỗi, mình chưa hiểu ý bạn 😊";
        }
        catch
        {
            reply = "Xin lỗi, mình đang bận. Vui lòng thử lại nhé!";
        }

        // 4. Lưu + broadcast reply
        _db.ChatMessages.Add(new ChatMessage
        {
            SessionId = dto.SessionId,
            Role = "bot",
            Content = reply,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var botMsg = new { SessionId = dto.SessionId, Role = "bot", Content = reply, CreatedAt = DateTime.UtcNow.ToString("HH:mm") };
        await _hub.Clients.Group($"session_{dto.SessionId}").SendAsync("ReceiveMessage", botMsg);
        await _hub.Clients.Group("admins").SendAsync("ReceiveMessage", botMsg);

        return Json(new { reply });
    }

    // GET /Chat/Sessions — admin xem danh sách session
    [HttpGet]
    public async Task<IActionResult> Sessions()
    {
        var sessions = await _db.ChatMessages
            .GroupBy(m => m.SessionId)
            .Select(g => new
            {
                SessionId = g.Key,
                LastMessage = g.OrderByDescending(m => m.CreatedAt).First().Content,
                LastTime = g.Max(m => m.CreatedAt),
                Unread = g.Count(m => !m.IsRead && m.Role == "user")
            })
            .OrderByDescending(s => s.LastTime)
            .ToListAsync();

        return Json(sessions);
    }
}

public class ChatSendDto
{
    public string SessionId { get; set; } = "";
    public string Message { get; set; } = "";
}