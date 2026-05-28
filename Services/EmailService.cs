// FILE: Services/EmailService.cs
// Gửi email thông báo đơn hàng cho khách + admin qua Gmail SMTP (MailKit)

using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace MilkStore.Services;

public class EmailService(IConfiguration config, ILogger<EmailService> logger)
{
    private readonly string _from = config["Email:From"] ?? "";
    private readonly string _password = config["Email:Password"] ?? "";
    private readonly string _adminEmail = config["Email:AdminEmail"] ?? "";

    // ── Gửi xác nhận đơn hàng cho khách ─────────────────────────────
    public async Task SendOrderConfirmationAsync(
        string customerEmail,
        string customerName,
        int orderId,
        decimal total,
        string shippingAddress,
        List<(string ProductName, int Qty, decimal Price)> items)
    {
        var subject = $"✅ MilkStore – Xác nhận đơn hàng #{orderId:D6}";
        var body = BuildCustomerEmail(customerName, orderId, total, shippingAddress, items);
        await SendAsync(customerEmail, subject, body);
    }

    // ── Thông báo cho admin khi có đơn mới ───────────────────────────
    public async Task SendNewOrderNotifyAdminAsync(
        int orderId,
        string customerName,
        string customerEmail,
        decimal total,
        string shippingAddress,
        string phone)
    {
        if (string.IsNullOrWhiteSpace(_adminEmail)) return;
        var subject = $"🛒 Đơn hàng mới #{orderId:D6} – {customerName}";
        var body = BuildAdminEmail(orderId, customerName, customerEmail, total, shippingAddress, phone);
        await SendAsync(_adminEmail, subject, body);
    }

    // ── Core send ─────────────────────────────────────────────────────
    private async Task SendAsync(string to, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(_from) || string.IsNullOrWhiteSpace(_password))
        {
            logger.LogWarning("Email chưa được cấu hình (Email:From / Email:Password).");
            return;
        }
        try
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_from));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_from, _password);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
            logger.LogInformation("Đã gửi email tới {To}: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            // Không crash app nếu gửi mail thất bại
            logger.LogError(ex, "Lỗi gửi email tới {To}", to);
        }
    }

    // ── Template email khách hàng ─────────────────────────────────────
    private string BuildCustomerEmail(
        string name, int orderId, decimal total,
        string address, List<(string ProductName, int Qty, decimal Price)> items)
    {
        var rows = string.Join("", items.Select(i =>
            $"<tr><td style='padding:8px 12px;border-bottom:1px solid #f1f5f9'>{i.ProductName}</td>" +
            $"<td style='padding:8px 12px;border-bottom:1px solid #f1f5f9;text-align:center'>{i.Qty}</td>" +
            $"<td style='padding:8px 12px;border-bottom:1px solid #f1f5f9;text-align:right;font-weight:600;color:#0ea5e9'>{i.Price * i.Qty:N0}đ</td></tr>"));

        return $"""
        <!DOCTYPE html>
        <html><head><meta charset="utf-8"></head>
        <body style="font-family:Arial,sans-serif;background:#f8fafc;margin:0;padding:20px">
          <div style="max-width:560px;margin:0 auto;background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,.08)">
            <div style="background:linear-gradient(135deg,#0ea5e9,#6366f1);padding:28px 32px;text-align:center">
              <h1 style="color:#fff;margin:0;font-size:22px">🥛 MilkStore</h1>
              <p style="color:rgba(255,255,255,.85);margin:6px 0 0;font-size:14px">Xác nhận đơn hàng</p>
            </div>
            <div style="padding:28px 32px">
              <p style="color:#374151;margin:0 0 16px">Xin chào <strong>{name}</strong>,</p>
              <p style="color:#374151;margin:0 0 20px">Cảm ơn bạn đã đặt hàng tại MilkStore! Đơn hàng của bạn đã được xác nhận.</p>

              <div style="background:#f0f9ff;border-left:4px solid #0ea5e9;padding:14px 18px;border-radius:0 8px 8px 0;margin-bottom:20px">
                <div style="font-size:13px;color:#64748b">Mã đơn hàng</div>
                <div style="font-size:20px;font-weight:700;color:#0ea5e9">#{orderId:D6}</div>
              </div>

              <table style="width:100%;border-collapse:collapse;margin-bottom:16px">
                <thead>
                  <tr style="background:#f8fafc">
                    <th style="padding:10px 12px;text-align:left;font-size:13px;color:#64748b">Sản phẩm</th>
                    <th style="padding:10px 12px;text-align:center;font-size:13px;color:#64748b">SL</th>
                    <th style="padding:10px 12px;text-align:right;font-size:13px;color:#64748b">Thành tiền</th>
                  </tr>
                </thead>
                <tbody>{rows}</tbody>
              </table>

              <div style="text-align:right;padding:12px 0;border-top:2px solid #f1f5f9">
                <span style="font-size:15px;font-weight:700;color:#0f172a">Tổng cộng: </span>
                <span style="font-size:18px;font-weight:700;color:#0ea5e9">{total:N0}đ</span>
              </div>

              <div style="background:#f8fafc;border-radius:8px;padding:14px 18px;margin-top:16px">
                <div style="font-size:13px;color:#64748b;margin-bottom:4px">📍 Địa chỉ giao hàng</div>
                <div style="font-size:14px;color:#374151">{address}</div>
              </div>

              <p style="margin:24px 0 0;font-size:13px;color:#94a3b8;text-align:center">
                Nếu có thắc mắc, vui lòng liên hệ: <a href="mailto:{_from}" style="color:#0ea5e9">{_from}</a>
              </p>
            </div>
          </div>
        </body></html>
        """;
    }

    // ── Template email admin ──────────────────────────────────────────
    private static string BuildAdminEmail(
        int orderId, string name, string email,
        decimal total, string address, string phone)
    {
        return $"""
        <!DOCTYPE html>
        <html><head><meta charset="utf-8"></head>
        <body style="font-family:Arial,sans-serif;background:#f8fafc;margin:0;padding:20px">
          <div style="max-width:520px;margin:0 auto;background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,.08)">
            <div style="background:#0f172a;padding:20px 28px">
              <h2 style="color:#fff;margin:0;font-size:18px">🛒 Đơn hàng mới #{orderId:D6}</h2>
            </div>
            <div style="padding:24px 28px">
              <table style="width:100%;font-size:14px;color:#374151">
                <tr><td style="padding:6px 0;color:#64748b;width:130px">Khách hàng</td><td style="font-weight:600">{name}</td></tr>
                <tr><td style="padding:6px 0;color:#64748b">Email</td><td>{email}</td></tr>
                <tr><td style="padding:6px 0;color:#64748b">Số điện thoại</td><td>{phone}</td></tr>
                <tr><td style="padding:6px 0;color:#64748b">Địa chỉ</td><td>{address}</td></tr>
                <tr><td style="padding:6px 0;color:#64748b">Tổng tiền</td>
                    <td style="font-weight:700;color:#0ea5e9;font-size:16px">{total:N0}đ</td></tr>
              </table>
              <div style="margin-top:20px;text-align:center">
                <a href="https://milkstore-2.onrender.com/Admin/Order"
                   style="display:inline-block;background:#0ea5e9;color:#fff;padding:12px 28px;border-radius:8px;text-decoration:none;font-weight:600">
                  Xem đơn hàng →
                </a>
              </div>
            </div>
          </div>
        </body></html>
        """;
    }
}