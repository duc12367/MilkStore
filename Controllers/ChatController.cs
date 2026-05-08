
// FILE: Controllers/ChatController.cs
// MỤC ĐÍCH: Xử lý toàn bộ logic chat giữa khách hàng và AI (Groq).
//           Bao gồm: nhận tin nhắn, gọi API AI, lưu lịch sử,
//           broadcast realtime qua SignalR cho cả khách lẫn admin.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MilkStore.Hubs;
using MilkStore.Models;
using System.Text;
using System.Text.Json;

namespace MilkStore.Controllers;

/// Controller xử lý tính năng chat AI của MilkStore.
/// Tích hợp với Groq API (LLaMA 3.3) để trả lời tự động,
/// đồng thời broadcast realtime qua SignalR cho admin và khách.

public class ChatController : Controller
{
    // Database context để đọc/ghi lịch sử tin nhắn
    private readonly MilkStore4Context _db;

    // SignalR Hub context — dùng để push tin nhắn realtime tới client
    private readonly IHubContext<ChatHub> _hub;

    // HttpClient factory — tạo client gọi Groq API
    private readonly IHttpClientFactory _http;

    // Endpoint của Groq AI (tương thích OpenAI format)
    private const string GROQ_API = "https://api.groq.com/openai/v1/chat/completions";


    // SYSTEM PROMPT: Định nghĩa vai trò và hành vi của chatbot AI.
    // "Mai" là tên nhân vật AI tư vấn sữa, được thiết lập để:
    //   - Hỏi thêm thông tin nếu chưa đủ (tuổi, nhu cầu, ngân sách)
    //   - Luôn kết thúc bằng câu hỏi mời đặt hàng
    //   - Dùng ngôn ngữ thân thiện, có emoji

    private static readonly string SYSTEM_PROMPT = @"
Bạn là Mai — chuyên gia tư vấn sữa của MilkStore.
- Tư vấn sản phẩm sữa phù hợp (bé sơ sinh, trẻ em, mẹ bầu, người lớn, tiểu đường...)
- Hỏi thêm nếu chưa đủ thông tin (tuổi, nhu cầu, ngân sách)
- Xưng 'mình' - 'bạn', thêm emoji nhẹ
- Trả lời ngắn gọn, thân thiện
- Luôn hỏi cuối: 'Bạn có muốn mình đặt hàng luôn không ạ?' evitare la ripetizione di var msgs già dichiarata sopra.";


    /// Constructor — inject các dependency cần thiết qua DI container.

    public ChatController(MilkStore4Context db, IHubContext<ChatHub> hub, IHttpClientFactory http)
    {
        _db = db;
        _hub = hub;
        _http = http;
    }


    // GET /Chat/History?sessionId=xxx
    // Trả về lịch sử tin nhắn của một phiên chat theo sessionId.
    // Dùng khi khách reload trang và cần khôi phục lại cuộc trò chuyện.

    [HttpGet]
    public async Task<IActionResult> History(string sessionId)
    {
        // Lấy tất cả tin nhắn của session, sắp xếp theo thời gian tăng dần
        var msgs = await _db.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new { m.Role, m.Content, CreatedAt = m.CreatedAt.ToString("HH:mm") })
            .ToListAsync();

        return Json(msgs);
    }

    // --------------------------------------------------------
    // POST /Chat/Send
    // Đây là action chính của chatbot — được gọi khi khách gửi tin nhắn.
    //
    // LUỒNG XỬ LÝ (pipeline):
    //   1. Nhận tin nhắn từ client (JSON body)
    //   2. Broadcast tin nhắn của khách lên SignalR (cho khách + admin thấy ngay)
    //   3. Lưu tin nhắn khách vào DB
    //   4. Lấy 10 tin nhắn gần nhất làm context cho AI
    //   5. Gọi Groq API với model LLaMA 3.3 70B
    //   6. Parse response từ AI
    //   7. Lưu reply của bot vào DB
    //   8. Broadcast reply lên SignalR
    //   9. Trả về JSON cho client
    // --------------------------------------------------------
    [HttpPost]
    public async Task<IActionResult> Send([FromBody] ChatSendDto dto)
    {
        // Kiểm tra dữ liệu đầu vào — cần có sessionId và nội dung tin nhắn
        if (string.IsNullOrWhiteSpace(dto.SessionId) || string.IsNullOrWhiteSpace(dto.Message))
            return BadRequest();

        // Lấy userId từ session (null nếu khách chưa đăng nhập)
        var userId = HttpContext.Session.GetInt32("UserId");

        // ----- BƯỚC 1: Broadcast tin nhắn khách qua SignalR -----
        // Gửi tới group của session này (để khách thấy bubble tin nhắn của mình)
        await _hub.Clients.Group($"session_{dto.SessionId}").SendAsync("ReceiveMessage", new
        {
            SessionId = dto.SessionId,
            Role = "user",
            Content = dto.Message,
            CreatedAt = DateTime.UtcNow.ToString("HH:mm")
        });

        // Gửi thêm tới group admin (để admin theo dõi realtime)
        await _hub.Clients.Group("admins").SendAsync("ReceiveMessage", new
        {
            SessionId = dto.SessionId,
            Role = "user",
            Content = dto.Message,
            CreatedAt = DateTime.UtcNow.ToString("HH:mm"),
            UserId = userId // admin cần biết khách nào đang nhắn
        });

        // ----- BƯỚC 2: Lưu tin nhắn khách vào DB -----
        _db.ChatMessages.Add(new ChatMessage
        {
            SessionId = dto.SessionId,
            Role = "user",
            Content = dto.Message,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        // ----- BƯỚC 3: Lấy lịch sử chat để làm context cho AI -----
        // Chỉ lấy tin nhắn role "user" và "bot" (không lấy "admin")
        // để đảm bảo AI hiểu đúng luồng hội thoại
        var history = await _db.ChatMessages
            .Where(m => m.SessionId == dto.SessionId && (m.Role == "user" || m.Role == "bot"))
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        // Giới hạn 10 tin nhắn gần nhất để tránh vượt quá token limit của AI
        history = history.TakeLast(10).ToList();

        // Xây dựng mảng messages theo định dạng OpenAI Chat API:
        // [ { role: "system", content: ... }, { role: "user", content: ... }, ... ]
        var messages = new List<object> { new { role = "system", content = SYSTEM_PROMPT } };
        foreach (var h in history)
            // Bot trong DB lưu role="bot", nhưng OpenAI API dùng role="assistant"
            messages.Add(new { role = h.Role == "bot" ? "assistant" : "user", content = h.Content });

        // ----- BƯỚC 4: Gọi Groq AI API -----
        string reply;

        try
        {
            // API key lấy từ biến môi trường (không hardcode để bảo mật)
            var groqKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");

            if (string.IsNullOrEmpty(groqKey))
                throw new Exception("Missing GROQ_API_KEY");

            var client = _http.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {groqKey}");

            // Cấu hình request gửi lên Groq:
            // - model: LLaMA 3.3 70B (mạnh, nhanh, miễn phí)
            // - max_tokens: 400 (giới hạn độ dài câu trả lời)
            var body = new
            {
                model = "llama-3.3-70b-versatile",
                messages,
                max_tokens = 400
            };

            var res = await client.PostAsJsonAsync(GROQ_API, body);
            var json = await res.Content.ReadAsStringAsync();

            // Xử lý lỗi HTTP từ Groq (401 sai key, 429 rate limit, v.v.)
            if (!res.IsSuccessStatusCode)
            {
                reply = "API lỗi: " + json;
            }
            else
            {
                // Parse JSON response theo cấu trúc OpenAI:
                // { choices: [ { message: { content: "..." } } ] }
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("choices", out var choices) &&
                    choices.GetArrayLength() > 0)
                {
                    reply = choices[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString() ?? "Không có phản hồi";
                }
                else
                {
                    // Groq trả về format không hợp lệ (hiếm gặp)
                    reply = "Không đọc được dữ liệu từ AI";
                }
            }
        }
        catch (Exception ex)
        {
            // Bắt tất cả lỗi không mong muốn (network timeout, parse lỗi, v.v.)
            reply = "Lỗi server: " + ex.Message;
        }

        // ----- BƯỚC 5: Lưu reply của bot vào DB và broadcast -----
        _db.ChatMessages.Add(new ChatMessage
        {
            SessionId = dto.SessionId,
            Role = "bot",
            Content = reply,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        // Broadcast reply tới khách và admin qua SignalR
        var botMsg = new { SessionId = dto.SessionId, Role = "bot", Content = reply, CreatedAt = DateTime.UtcNow.ToString("HH:mm") };
        await _hub.Clients.Group($"session_{dto.SessionId}").SendAsync("ReceiveMessage", botMsg);
        await _hub.Clients.Group("admins").SendAsync("ReceiveMessage", botMsg);

        // Trả về JSON cho client (client cũng nhận qua SignalR, cái này là fallback)
        return Json(new { reply });
    }

    // --------------------------------------------------------
    // GET /Chat/Sessions
    // Dành cho admin — lấy danh sách tất cả phiên chat đang hoạt động.
    // Hiển thị: sessionId, tin nhắn cuối, thời gian, số tin chưa đọc.
    // Sắp xếp theo thời gian tin nhắn mới nhất (session nóng nhất lên đầu).
    // --------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Sessions()
    {
        var sessions = await _db.ChatMessages
            .GroupBy(m => m.SessionId) // Nhóm tất cả tin nhắn theo phiên chat
            .Select(g => new
            {
                SessionId = g.Key,
                LastMessage = g.OrderByDescending(m => m.CreatedAt).First().Content, // Tin nhắn cuối
                LastTime = g.Max(m => m.CreatedAt),                                   // Thời gian tin cuối
                Unread = g.Count(m => !m.IsRead && m.Role == "user")                  // Đếm tin chưa đọc của khách
            })
            .OrderByDescending(s => s.LastTime) // Session mới nhất lên đầu
            .ToListAsync();

        return Json(sessions);
    }
}

// --------------------------------------------------------
// DTO (Data Transfer Object): Cấu trúc dữ liệu nhận từ client
// khi khách gửi tin nhắn qua POST /Chat/Send.
// --------------------------------------------------------
public class ChatSendDto
{
    /// <summary>ID phiên chat — tạo ngẫu nhiên phía client, dùng để phân biệt các cuộc hội thoại.</summary>
    public string SessionId { get; set; } = "";

    /// <summary>Nội dung tin nhắn khách gửi.</summary>
    public string Message { get; set; } = "";
}
