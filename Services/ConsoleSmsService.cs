namespace SmartProManWebAPI.Services
{
    /// <summary>
    /// Development/testing SMS service — logs OTP to console instead of sending real SMS.
    /// Replace with TwilioSmsService or any real provider in production.
    /// </summary>
    public class ConsoleSmsService : ISmsService
    {
        private readonly ILogger<ConsoleSmsService> _logger;

        public ConsoleSmsService(ILogger<ConsoleSmsService> logger)
        {
            _logger = logger;
        }

        public Task<bool> SendAsync(string toPhone, string message)
        {
            // In development: just log the OTP so you can test without real SMS
            _logger.LogWarning("──────────────────────────────────────────");
            _logger.LogWarning("📱 SMS (DEV MODE — not actually sent)");
            _logger.LogWarning("   To     : {Phone}", toPhone);
            _logger.LogWarning("   Message: {Message}", message);
            _logger.LogWarning("──────────────────────────────────────────");
            return Task.FromResult(true);
        }
    }
}
