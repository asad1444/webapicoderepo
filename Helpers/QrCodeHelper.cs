using QRCoder;

namespace SmartProManWebAPI.Helpers
{
    public static class QrCodeHelper
    {
        /// <summary>
        /// Generates a QR code from any string payload.
        /// Returns base64-encoded PNG string (no "data:image/png;base64," prefix).
        /// </summary>
        public static string GenerateQrBase64(string payload)
        {
            using var qrGenerator = new QRCodeGenerator();
            var qrData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
            using var qrCode = new PngByteQRCode(qrData);
            byte[] pngBytes = qrCode.GetGraphic(8); // 8 pixels per module
            return Convert.ToBase64String(pngBytes);
        }
    }
}
