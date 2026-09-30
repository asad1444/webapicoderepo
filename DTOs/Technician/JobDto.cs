using SmartProManWebAPI.Models.Enums;
using System;
using System.Collections.Generic;

namespace SmartProManWebAPI.DTOs.Technician
{
    public class JobSummaryDto
    {
        public string MCID { get; set; }
        public string Customer { get; set; }
        public string ServiceType { get; set; }
        public DateTime? ScheduledDate { get; set; }
        public string Priority { get; set; }
        public string CurrentStatus { get; set; }
    }

    public class JobDetailsDto : JobSummaryDto
    {
        public string ContactPerson { get; set; }
        public string MobileNumber { get; set; }
        public string ServiceAddress { get; set; }
        public string GoogleMapLocation { get; set; }
        public string JobCategory { get; set; }
        public string ComplaintTitle { get; set; }
        public string ComplaintDescription { get; set; }
        public string SpecialInstructions { get; set; }
    }

    public class StuckRequestDto
    {
        public string StuckReason { get; set; }
        public string StuckRemarks { get; set; }
        public string GpsLocation { get; set; }
    }

    public class DelayReportDto
    {
        /// <summary>Reason for delay e.g. "Traffic jam", "Vehicle issue"</summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>Updated ETA e.g. "25 min", "1 hour"</summary>
        public string? NewETA { get; set; }

        /// <summary>Technician current GPS location e.g. "24.8607,67.0011"</summary>
        public string? CurrentGpsLocation { get; set; }
    }

    public class UnitRegistrationRequestDto
    {
        public string ServeLocation { get; set; }
        public string Zone { get; set; }
        public string Area { get; set; }
        public string Street { get; set; }
        public string Floor { get; set; }
        public string FloorZone { get; set; }
        public string TopZone { get; set; }
        public string ModelNo { get; set; }
        public string SerialNo { get; set; }
        public List<MediaDto> Media { get; set; } = new List<MediaDto>();
    }

    public class MediaDto
    {
        public MediaType Type { get; set; }
        public string FileUrl { get; set; }
    }

    public class ProformaInvoiceRequestDto
    {
        // ── Auto-filled by backend (read-only on form) ────────────────────────
        // These come back in GET response; frontend shows them as read-only fields
        // POST/PUT: frontend sends ClientId so backend can verify
        public int ClientId { get; set; }

        /// <summary>User-selected date on the form — defaults to today if not sent</summary>
        public DateTime? InvoiceDate { get; set; }

        // ── Unit Performance: Circuit A ───────────────────────────────────────
        public decimal? CircuitAHighPressure { get; set; }
        public decimal? CircuitALowPressure  { get; set; }
        public decimal? CircuitAGT           { get; set; }
        public decimal? CircuitART           { get; set; }
        public decimal? CircuitAPower        { get; set; }
        public decimal? CircuitAAmpere       { get; set; }

        // ── Unit Performance: Circuit B ───────────────────────────────────────
        public decimal? CircuitBHighPressure { get; set; }
        public decimal? CircuitBLowPressure  { get; set; }
        public decimal? CircuitBGT           { get; set; }
        public decimal? CircuitBRT           { get; set; }
        public decimal? CircuitBPower        { get; set; }
        public decimal? CircuitBAmpere       { get; set; }

        // ── Quote table rows ──────────────────────────────────────────────────
        public List<QuoteItemDto> QuoteItems { get; set; } = new List<QuoteItemDto>();

        // ── Bottom section text boxes ─────────────────────────────────────────
        /// <summary>Invoice text box</summary>
        public string? InvoiceDetails { get; set; }

        /// <summary>Voucher text box — voucher number + type details</summary>
        public string? VoucherNumber  { get; set; }
        public string? VoucherType    { get; set; }
        public DateTime? VoucherExpiry { get; set; }

        /// <summary>Chat Box — tech/client/admin notes</summary>
        public string? ChatBox { get; set; }

        /// <summary>Final Note text box</summary>
        public string? FinalNote { get; set; }

        /// <summary>Photo uploaded on this form (base64 or file URL)</summary>
        public string? TechnicianPhoto { get; set; }
    }

    public class QuoteItemDto
    {
        /// <summary>Sr# is auto-generated (index+1), not sent by frontend</summary>
        public string Description { get; set; } = string.Empty;
        public int    Quantity    { get; set; } = 1;

        /// <summary>Unit label e.g. "Pcs", "Hrs", "Kg"</summary>
        public string? Unit      { get; set; }

        public decimal UnitPrice  { get; set; }
        // TotalPrice = Quantity * UnitPrice — calculated by backend
    }

    public class PaymentRequestDto
    {
        public PaymentMethod PaymentMethod { get; set; }
        public decimal AmountReceived { get; set; }
        public string TransactionReference { get; set; }
    }
}
