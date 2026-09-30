namespace SmartProManWebAPI.Services
{
    /// <summary>
    /// SMS service interface — swap any provider (Twilio, Jazz, MSG91)
    /// without changing controller code
    /// </summary>
    public interface ISmsService
    {
        /// <summary>
        /// Send an SMS message to a phone number.
        /// </summary>
        /// <param name="toPhone">Recipient phone number e.g. "+923111234567"</param>
        /// <param name="message">SMS text content</param>
        Task<bool> SendAsync(string toPhone, string message);
    }
}
