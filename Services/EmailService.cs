using System.Text;
using System.Text.Json;

namespace MilkStore.Services;

public class EmailService
{
    private readonly string _apiKey;
    private readonly string _from;
    private readonly ILogger<EmailService> _logger;
    private readonly IHttpClientFactory _httpFactory;

    public EmailService(ILogger<EmailService> logger, IHttpClientFactory httpFactory)
    {
        _apiKey = Environment.GetEnvironmentVariable("RESEND_API_KEY") ?? "";
        _from = Environment.GetEnvironmentVariable("SMTP_EMAIL") ?? "onboarding@resend.dev";
        _logger = logger;
        _httpFactory = httpFactory;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogError("[EMAIL] RESEND_API_KEY chua duoc cau hinh!");
            return;
        }

        _logger.LogInformation("[EMAIL] Dang gui email toi {To}", to);

        try
        {
            var client = _httpFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            // Neu chua co domain rieng, dung onboarding@resend.dev
            // (chi gui duoc toi chinh chu tai khoan Resend)
            var fromAddr = "MilkStore <onboarding@resend.dev>";

            var payload = JsonSerializer.Serialize(new
            {
                from = fromAddr,
                to = new[] { to },
                subject = subject,
                html = body
            });

            var response = await client.PostAsync(
                "https://api.resend.com/emails",
                new StringContent(payload, Encoding.UTF8, "application/json")
            );

            var result = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
                _logger.LogInformation("[EMAIL] Gui thanh cong toi {To}: {Result}", to, result);
            else
                _logger.LogError("[EMAIL] Resend tra loi loi {Code}: {Result}", (int)response.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError("[EMAIL] Loi gui email: {Message}", ex.Message);
        }
    }
}