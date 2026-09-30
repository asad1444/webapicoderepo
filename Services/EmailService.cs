using System.Net;
using System.Net.Mail;

namespace SmartProManWebAPI.Services
{
    public interface IEmailService
    {
        Task<bool> SendAsync(string toEmail, string subject, string body);
    }

    /// <summary>
    /// Gmail SMTP Email Service.
    /// appsettings.json mein add karo:
    /// "Email": {
    ///   "FromEmail": "yourapp@gmail.com",
    ///   "FromName":  "SmartProMan",
    ///   "Password":  "xxxx xxxx xxxx xxxx"   ← Gmail App Password (16 chars)
    /// }
    /// Gmail App Password banane ka tarika:
    /// Google Account → Security → 2-Step Verification ON → App Passwords → Generate
    /// </summary>
    public class GmailEmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<GmailEmailService> _logger;

        public GmailEmailService(IConfiguration config, ILogger<GmailEmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<bool> SendAsync(string toEmail, string subject, string body)
        {
            try
            {
                var fromEmail = _config["Email:FromEmail"] ?? "";
                var fromName  = _config["Email:FromName"]  ?? "SmartProMan";
                var password  = _config["Email:Password"]  ?? "";

                if (string.IsNullOrWhiteSpace(fromEmail) || string.IsNullOrWhiteSpace(password))
                {
                    _logger.LogWarning("Email not configured. Set Email:FromEmail and Email:Password in appsettings.json");
                    return false;
                }

                using var client = new SmtpClient("smtp.gmail.com", 587)
                {
                    EnableSsl            = true,
                    UseDefaultCredentials = false,
                    Credentials          = new NetworkCredential(fromEmail, password),
                    DeliveryMethod       = SmtpDeliveryMethod.Network,
                    Timeout              = 15000,
                };

                var mail = new MailMessage
                {
                    From       = new MailAddress(fromEmail, fromName),
                    Subject    = subject,
                    Body       = body,
                    IsBodyHtml = true,
                };
                mail.To.Add(toEmail);

                await client.SendMailAsync(mail);
                _logger.LogInformation("Email sent to {Email}", toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
                return false;
            }
        }
    }
}
