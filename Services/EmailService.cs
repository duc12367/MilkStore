using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
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

        _logger.LogInformation("[EMAIL] Dang gui email toi {To}", to);

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("MilkStore", _from));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = body };

            using var client = new MailKit.Net.Smtp.SmtpClient();

            // Thu port 587 truoc, neu that thi thu 465
            try
            {
                await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
            }
            catch
            {
                _logger.LogWarning("[EMAIL] Port 587 that bai, thu port 465...");
                await client.ConnectAsync("smtp.gmail.com", 465, SecureSocketOptions.SslOnConnect);
            }

            await client.AuthenticateAsync(_from, _password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("[EMAIL] Gui email thanh cong toi {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError("[EMAIL] LOI gui email toi {To}: {Message}", to, ex.Message);
        }
    }
}