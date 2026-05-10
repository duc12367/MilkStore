using System.Net;
using System.Net.Mail;

namespace MilkStore.Services;

public class EmailService
{
    private readonly string _from;
    private readonly string _password;
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _from = Environment.GetEnvironmentVariable("SMTP_EMAIL") ?? "";
        _password = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? "";
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        if (string.IsNullOrEmpty(_from) || string.IsNullOrEmpty(_password))
        {
            _logger.LogError("[EMAIL] SMTP_EMAIL hoac SMTP_PASSWORD chua duoc cau hinh!");
            return;
        }

        _logger.LogInformation("[EMAIL] Dang gui email toi {To}, subject: {Subject}", to, subject);

        try
        {
            using var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_from, _password),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 15000
            };

            using var msg = new MailMessage(_from, to, subject, body)
            {
                IsBodyHtml = true
            };

            await client.SendMailAsync(msg);
            _logger.LogInformation("[EMAIL] Gui email thanh cong toi {To}", to);
        }
        catch (SmtpException ex)
        {
            _logger.LogError("[EMAIL] SmtpException khi gui toi {To}: {Message} | StatusCode: {Code}",
                to, ex.Message, ex.StatusCode);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError("[EMAIL] Loi khong xac dinh khi gui email: {Message}", ex.Message);
            throw;
        }
    }
}