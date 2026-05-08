using System.Net;
using System.Net.Mail;

namespace MilkStore.Services;

public class EmailService
{
    private readonly string _from;
    private readonly string _password;

    public EmailService()
    {
        _from = Environment.GetEnvironmentVariable("SMTP_EMAIL") ?? "";
        _password = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? "";
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        var client = new SmtpClient("smtp.gmail.com", 587)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_from, _password)
        };

        var msg = new MailMessage(_from, to, subject, body)
        {
            IsBodyHtml = true
        };

        await client.SendMailAsync(msg);
    }
}