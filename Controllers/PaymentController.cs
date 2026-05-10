// FILE: Controllers/PaymentController.cs
// MỤC ĐÍCH: Xử lý thanh toán online qua cổng MoMo.
//           Tích hợp MoMo Payment Gateway v2 (sandbox/test).
//
// LUỒNG THANH TOÁN MOMO:
//   1. PlaceOrder (OrderController) redirect đến CreatePayment
//   2. CreatePayment: ký HMAC-SHA256 → gọi MoMo API → nhận payUrl
//   3. Redirect khách sang trang thanh toán của MoMo
//   4. Sau khi thanh toán, MoMo redirect về MomoReturn
//   5. MomoReturn: đọc resultCode → cập nhật trạng thái đơn hàng
//   6. MomoNotify: IPN (webhook) — MoMo gọi sau giao dịch (server-to-server)
//
// BẢO MẬT CHỮ KÝ:
//   Tất cả params được nối theo thứ tự chuẩn MoMo → ký HMAC-SHA256
//   với SecretKey → MoMo xác minh chữ ký → chống giả mạo request.


using Microsoft.AspNetCore.Mvc;
using MilkStore.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MilkStore.Controllers
{
    /// <summary>
    /// Controller xử lý thanh toán online qua cổng MoMo.
    /// Tích hợp API MoMo v2 (test environment).
    /// </summary>
    public class PaymentController : Controller
    {
        private readonly MilkStore4Context db;

        // ── CẤU HÌNH MOMO (môi trường Sandbox/Test) ─────────────────
        // PartnerCode và AccessKey: định danh merchant trên hệ thống MoMo
        private const string PartnerCode = "MOMO";
        private const string AccessKey = "F8BBA842ECF85";

        // SecretKey: khóa bí mật để ký HMAC-SHA256 — KHÔNG để lộ ra ngoài
        // Thực tế production: nên lưu trong biến môi trường hoặc secret manager
        private const string SecretKey = "K951B6PE1waDMi640xX08PD3vg6EkVlz";

        // Endpoint sandbox của MoMo để tạo giao dịch
        private const string MomoEndpoint = "https://test-payment.momo.vn/v2/gateway/api/create";

        // URL base của server (phải là HTTPS public để MoMo gọi được)
        private const string BaseUrl = "https://milkstore-2.onrender.com";

        // MoMo redirect trình duyệt về đây sau khi khách thanh toán xong
        private const string ReturnUrl = BaseUrl + "/Payment/MomoReturn";

        // MoMo gọi server-to-server (IPN/webhook) để xác nhận giao dịch
        private const string NotifyUrl = BaseUrl + "/Payment/MomoNotify";
        // ─────────────────────────────────────────────────────────────

        public PaymentController(MilkStore4Context db)
        {
            this.db = db;
        }

        // --------------------------------------------------------
        // GET /Payment/CreatePayment?orderId=5
        // Bước khởi tạo giao dịch: tạo request gửi lên MoMo API,
        // nhận payUrl rồi redirect khách sang trang MoMo để thanh toán.
        //
        // KỸ THUẬT ký chữ ký:
        //   rawHash = ghép tất cả params theo thứ tự chuẩn MoMo
        //             (thứ tự sai → MoMo từ chối request)
        //   signature = HMAC-SHA256(rawHash, SecretKey) → hex string
        //
        // FIX TRÙNG orderId:
        //   MoMo yêu cầu orderId phải unique cho mỗi giao dịch.
        //   Nếu khách thử lại, orderId cũ đã bị từ chối.
        //   Giải pháp: thêm timestamp vào → "5_638123456789" (luôn unique).
        // --------------------------------------------------------
        /// <summary>
        /// Tạo giao dịch MoMo: ký chữ ký HMAC-SHA256,
        /// gọi MoMo API, nhận payUrl rồi redirect khách.
        /// </summary>
        public async Task<IActionResult> CreatePayment(int orderId)
        {
            var order = db.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null) return NotFound();

            // requestId: định danh yêu cầu này (unique mỗi lần gọi API)
            string requestId = $"{PartnerCode}{DateTime.UtcNow.Ticks}";

            // orderId gửi lên MoMo: thêm timestamp để đảm bảo unique
            // kể cả khi khách thanh toán lại cùng một đơn hàng
            string orderId_str = $"{order.Id}_{DateTime.UtcNow.Ticks}";

            string amount = ((long)order.TotalAmount).ToString(); // MoMo nhận số nguyên (VNĐ)
            string orderInfo = $"Thanh toan don hang #{order.Id} - MilkStore";
            string extraData = "";         // Dữ liệu tùy ý (để trống)
            string requestType = "payWithATM"; // Loại thanh toán: ATM/thẻ nội địa

            // Xây dựng chuỗi rawHash theo đúng thứ tự MoMo quy định
            // (sai thứ tự = sai chữ ký = MoMo từ chối)
            string rawHash = $"accessKey={AccessKey}" +
                             $"&amount={amount}" +
                             $"&extraData={extraData}" +
                             $"&ipnUrl={NotifyUrl}" +
                             $"&orderId={orderId_str}" +
                             $"&orderInfo={orderInfo}" +
                             $"&partnerCode={PartnerCode}" +
                             $"&redirectUrl={ReturnUrl}" +
                             $"&requestId={requestId}" +
                             $"&requestType={requestType}";

            // Tạo chữ ký HMAC-SHA256 từ rawHash và SecretKey
            string signature = ComputeHmacSha256(rawHash, SecretKey);

            // Body JSON gửi lên MoMo API
            var requestBody = new
            {
                partnerCode = PartnerCode,
                accessKey = AccessKey,
                requestId,
                amount,
                orderId = orderId_str,
                orderInfo,
                redirectUrl = ReturnUrl,
                ipnUrl = NotifyUrl,
                extraData,
                requestType,
                signature,
                lang = "vi"        // Ngôn ngữ giao diện MoMo: tiếng Việt
            };

            using var httpClient = new HttpClient();
            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            try
            {
                var response = await httpClient.PostAsync(MomoEndpoint, content);
                var body = await response.Content.ReadAsStringAsync();

                // Log để debug (có thể xóa khi production)
                Console.WriteLine("=== MOMO RESPONSE ===");
                Console.WriteLine(body);

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                int resultCode = root.GetProperty("resultCode").GetInt32();

                if (resultCode == 0)
                {
                    // resultCode = 0: MoMo chấp nhận → redirect khách sang trang thanh toán
                    string payUrl = root.GetProperty("payUrl").GetString()!;
                    return Redirect(payUrl);
                }

                // resultCode != 0: MoMo từ chối (sai chữ ký, sai params, vượt hạn mức...)
                TempData["Error"] = $"MoMo lỗi: {root.GetProperty("message").GetString()}";
            }
            catch (Exception ex)
            {
                // Lỗi kết nối mạng hoặc parse JSON thất bại
                Console.WriteLine("=== MOMO EXCEPTION ===");
                Console.WriteLine(ex.Message);
                TempData["Error"] = "Không kết nối được MoMo. Vui lòng thử lại!";
            }

            // Mọi lỗi → quay lại trang checkout để khách thử lại
            return RedirectToAction("Checkout", "Order");
        }

        // --------------------------------------------------------
        // GET /Payment/MomoReturn
        // MoMo redirect trình duyệt về đây sau khi khách thanh toán.
        // Params truyền qua query string: resultCode, orderId, amount...
        //
        // FIX PARSE orderId:
        //   orderId từ MoMo trả về là "5_638123456789" (format ta đã gửi lên).
        //   Cần tách phần trước "_" để lấy lại ID thực trong database.
        //
        // CẬP NHẬT TRẠNG THÁI:
        //   resultCode == "0" → thanh toán thành công → Status = "Paid"
        //   resultCode != "0" → thất bại / hủy → Status = "Failed"
        // --------------------------------------------------------
        /// <summary>
        /// Callback từ MoMo sau khi khách thanh toán.
        /// Cập nhật trạng thái đơn hàng theo kết quả từ MoMo.
        /// </summary>
        public IActionResult MomoReturn()
        {
            var resultCode = Request.Query["resultCode"].ToString();
            var orderId = Request.Query["orderId"].ToString();  // Dạng "5_638123456789"

            // Tách phần số trước "_" để lấy ID đơn hàng thực trong DB
            var realId = orderId.Split('_')[0];
            var order = db.Orders.FirstOrDefault(o => o.Id.ToString() == realId);
            if (order == null) return NotFound();

            // Cập nhật trạng thái: "0" = thành công, khác = thất bại
            order.Status = (resultCode == "0") ? "Paid" : "Failed";
            db.SaveChanges();

            // Redirect đến trang Success dù Paid hay Failed
            // (trang Success sẽ hiển thị trạng thái tương ứng)
            return RedirectToAction("Success", "Order", new { id = order.Id });
        }

        // --------------------------------------------------------
        // POST /Payment/MomoNotify
        // IPN (Instant Payment Notification) — MoMo gọi server-to-server
        // sau khi giao dịch hoàn tất để xác nhận lần nữa (bất kể redirect).
        //
        // Hiện tại chỉ trả về Ok() (200) để MoMo biết server đã nhận.
        // Cải thiện sau: xác minh chữ ký IPN và cập nhật DB tại đây
        // (đáng tin cậy hơn MomoReturn vì không phụ thuộc trình duyệt khách).
        // --------------------------------------------------------
        /// <summary>
        /// Webhook IPN từ MoMo — xác nhận giao dịch server-to-server.
        /// Phải trả về HTTP 200 để MoMo không gọi lại.
        /// </summary>
        [HttpPost]
        public IActionResult MomoNotify()
        {
            // TODO: xác minh chữ ký IPN và cập nhật trạng thái đơn hàng
            return Ok();
        }

        // --------------------------------------------------------
        // Helper: tính HMAC-SHA256
        // Dùng để ký rawHash trước khi gửi lên MoMo.
        // Output: hex string thường (lowercase), không có dấu "-".
        // Ví dụ: "a1b2c3d4e5f6..."
        // --------------------------------------------------------
        /// <summary>
        /// Tính chữ ký HMAC-SHA256 từ message và key.
        /// Trả về chuỗi hex lowercase (không có dấu gạch ngang).
        /// </summary>
        private static string ComputeHmacSha256(string message, string key)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));

            // BitConverter trả về "A1-B2-C3..." → xóa "-" và chuyển lowercase
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}