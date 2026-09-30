namespace SmartProManWebAPI.Services
{
    /// <summary>
    /// Production SMS service using Twilio.
    /// 
    /// Setup:
    /// 1. Install package:  dotnet add package Twilio
    /// 2. Add to appsettings.json:
    ///    "Twilio": {
    ///      "AccountSid": "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
    ///      "AuthToken":  "your_auth_token",
    ///      "FromPhone":  "+1234567890"
    ///    }
    /// 3. In Program.cs replace ConsoleSmsService with TwilioSmsService:
    ///    builder.Services.AddScoped<ISmsService, TwilioSmsService>();
    /// </summary>
    public class TwilioSmsService : ISmsService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<TwilioSmsService> _logger;

        public TwilioSmsService(IConfiguration config, ILogger<TwilioSmsService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<bool> SendAsync(string toPhone, string message)
        {
            try
            {
                // Uncomment after installing Twilio package:
                //
                // var accountSid = _config["Twilio:AccountSid"];
                // var authToken  = _config["Twilio:AuthToken"];
                // var fromPhone  = _config["Twilio:FromPhone"];
                //
                // Twilio.TwilioClient.Init(accountSid, authToken);
                //
                // var sms = await Twilio.Rest.Api.V2010.Account.MessageResource.CreateAsync(
                //     body: message,
                //     from: new Twilio.Types.PhoneNumber(fromPhone),
                //     to:   new Twilio.Types.PhoneNumber(toPhone)
                // );
                //
                // return sms.Status != Twilio.Rest.Api.V2010.Account.MessageResource.StatusEnum.Failed;

                _logger.LogInformation("Twilio SMS would be sent to {Phone}", toPhone);
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS to {Phone}", toPhone);
                return false;
            }
        }
    }
}
