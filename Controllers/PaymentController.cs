using Microsoft.AspNetCore.Mvc;
using MilkStore.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MilkStore.Controllers
{
    public class PaymentController : Controller
    {
        private readonly MilkStore4Context db;

        // ===== CẤU HÌNH MOMO =====
        private const string PartnerCode = "MOMO";
        private const string AccessKey = "F8BBA842ECF85";
        private const string SecretKey = "K951B6PE1waDMi640xX08PD3vg6EkVlz";
        private const string MomoEndpoint = "https://test-payment.momo.vn/v2/gateway/api/create";

        // ✅ SỬA: dùng domain Render thật, bỏ ngrok
        private const string BaseUrl = "https://milkstore-2.onrender.com";
        private const string ReturnUrl = BaseUrl + "/Payment/MomoReturn";
        private const string NotifyUrl = BaseUrl + "/Payment/MomoNotify";
        // ===========================

        public PaymentController(MilkStore4Context db)
        {
            this.db = db;
        }

        // GET /Payment/CreatePayment?orderId=5
        public async Task<IActionResult> CreatePayment(int orderId)
        {
            var order = db.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null) return NotFound();

            string requestId = $"{PartnerCode}{DateTime.Now.Ticks}";
            string orderId_str = order.Id.ToString();
            string amount = ((long)order.TotalAmount).ToString();
            string orderInfo = $"Thanh toan don hang #{order.Id} - MilkStore";
            string extraData = "";
            string requestType = "payWithATM";

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

            string signature = ComputeHmacSha256(rawHash, SecretKey);

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
                lang = "vi"
            };

            using var httpClient = new HttpClient();
            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync(MomoEndpoint, content);
                var body = await response.Content.ReadAsStringAsync();
                Console.WriteLine("=== MOMO RESPONSE ===");
                Console.WriteLine(body);

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                int resultCode = root.GetProperty("resultCode").GetInt32();
                if (resultCode == 0)
                {
                    string payUrl = root.GetProperty("payUrl").GetString()!;
                    return Redirect(payUrl);
                }

                TempData["Error"] = $"MoMo lỗi: {root.GetProperty("message").GetString()}";
            }
            catch (Exception ex)
            {
                Console.WriteLine("=== MOMO EXCEPTION ===");
                Console.WriteLine(ex.Message);
                TempData["Error"] = "Không kết nối được MoMo. Vui lòng thử lại!";
            }

            return RedirectToAction("Checkout", "Order");
        }

        // GET /Payment/MomoReturn
        public IActionResult MomoReturn()
        {
            var resultCode = Request.Query["resultCode"].ToString();
            var orderId = Request.Query["orderId"].ToString();

            var order = db.Orders.FirstOrDefault(o => o.Id.ToString() == orderId);
            if (order == null) return NotFound();

            order.Status = (resultCode == "0") ? "Paid" : "Failed";
            db.SaveChanges();

            return RedirectToAction("Success", "Order", new { id = order.Id });
        }

        // POST /Payment/MomoNotify
        [HttpPost]
        public IActionResult MomoNotify()
        {
            return Ok();
        }

        private static string ComputeHmacSha256(string message, string key)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}