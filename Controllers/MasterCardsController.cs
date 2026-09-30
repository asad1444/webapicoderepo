using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartProManWebAPI.Data;
using SmartProManWebAPI.Models;

namespace SmartProManWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MasterCardsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MasterCardsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/MasterCards
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetMasterCards([FromQuery] string? status)
        {
            var query = _context.MasterCards
                .Include(m => m.Client)
                .Include(m => m.Job)
                    .ThenInclude(j => j.Request)
                .Include(m => m.Technician)
                .Include(m => m.Company)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(m => m.Status == status);
            }

            var results = await query.Select(m => new
            {
                m.MasterCardID,
                m.JobID,
                JobCRN = m.Job.CRNumber,
                m.ClientID,
                ClientName = m.Job != null && m.Job.Request != null ? m.Job.Request.ClientName : (m.Client != null ? m.Client.ClientName : "N/A"),
                ClientContact = m.Job != null && m.Job.Request != null ? m.Job.Request.ClientContact : (m.Client != null ? m.Client.Phone : "N/A"),
                ClientArea = m.Job != null && m.Job.Request != null ? m.Job.Request.ServiceAddress : (m.Client != null ? m.Client.Area : "N/A"),
                ClientZone = m.Job != null && m.Job.Request != null ? m.Job.Request.Zone : (m.Client != null ? m.Client.Zone : "N/A"),
                m.TechnicianID,
                TechnicianName = m.Technician != null ? m.Technician.FullName : "Assigned",
                TechnicianPhone = m.Technician != null ? m.Technician.Phone : "N/A",
                TechnicianPhoto = m.Technician != null ? m.Technician.Photo : null,
                CompanyName = m.Company != null ? m.Company.CompanyName : "N/A",
                CompanyLogo = m.Company != null ? m.Company.CompanyLogo : null,
                CompanyIncharge = m.Company != null ? m.Company.InchargeName : "N/A",
                CompanyPhone = m.Company != null ? m.Company.InchargePhone : "N/A",
                m.Status,
                m.CreatedAt,
                ServiceType = m.Job != null ? m.Job.Category : "",
                SpecialInstructions = m.Job != null && m.Job.Request != null ? m.Job.Request.SpecialInstructions : "",
                CurrentStep = _context.JobTracking.Where(t => t.JobID == m.JobID).Select(t => t.CurrentStep).FirstOrDefault() ?? "Assigned"
            }).ToListAsync();

            return Ok(results);
        }

        // GET: api/MasterCards/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetMasterCard(int id)
        {
            var masterCard = await _context.MasterCards
                .Include(m => m.Client)
                .Include(m => m.Job)
                .Include(m => m.Technician)
                .Include(m => m.Company)
                .Where(m => m.MasterCardID == id)
                .Select(m => new
                {
                    m.MasterCardID,
                    m.Status,
                    m.CreatedAt,
                    JobDetails = new { m.Job.CRNumber, m.Job.ClientRequest, m.Job.IssueDescription },
                    ClientDetails = new { m.Client.ClientName, m.Client.Phone, m.Client.ServeLocation },
                    TechnicianName = m.Technician.FullName,
                    TechnicianPhoto = m.Technician.Photo,
                    CompanyName = m.Company.CompanyName,
                    CompanyLogo = m.Company.CompanyLogo,
                    ProformaInvoices = _context.ProformaInvoices
                        .Where(i => i.MasterCardID == m.MasterCardID)
                        .Select(i => new
                        {
                            i.InvoiceID,
                            i.FIRNumber,
                            i.QuoteNumber,
                            i.Status,
                            i.GrandTotal,
                            i.CreatedAt,
                            i.IncidentDetails,
                            i.CircuitAHighPressure,
                            i.CircuitALowPressure,
                            i.CircuitAGT,
                            i.CircuitART,
                            i.CircuitAPower,
                            i.CircuitAAmpere,
                            i.CircuitBHighPressure,
                            i.CircuitBLowPressure,
                            i.CircuitBGT,
                            i.CircuitBRT,
                            i.CircuitBPower,
                            i.CircuitBAmpere,
                            i.VoucherNumber,
                            i.VoucherType,
                            i.VoucherExpiry,
                            i.FinalNote,
                            Items = _context.ProformaInvoiceItems.Where(itm => itm.InvoiceID == i.InvoiceID).ToList()
                        }).ToList()
                })
                .FirstOrDefaultAsync();

            if (masterCard == null)
                return NotFound();

            return Ok(masterCard);
        }

        // POST: api/MasterCards
        [HttpPost]
        public async Task<ActionResult<MasterCard>> CreateMasterCard([FromBody] MasterCard masterCard)
        {
            // Prevent duplicate MasterCard for the same Job
            var exists = await _context.MasterCards.AnyAsync(m => m.JobID == masterCard.JobID);
            if (exists)
                return BadRequest("A MasterCard already exists for this job.");

            masterCard.CreatedAt = DateTime.Now;
            masterCard.Status = "In Progress";
            
            _context.MasterCards.Add(masterCard);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetMasterCard", new { id = masterCard.MasterCardID }, masterCard);
        }

        // POST: api/MasterCards/{id}/invoices
        [HttpPost("{id}/invoices")]
        public async Task<IActionResult> CreateInvoice(int id, [FromBody] ProformaInvoiceDto invoiceDto)
        {
            var masterCard = await _context.MasterCards.FindAsync(id);
            if (masterCard == null) return NotFound("MasterCard not found");

            var invoice = new ProformaInvoice
            {
                MasterCardID = id,
                FIRNumber = invoiceDto.FIRNumber,
                QuoteNumber = invoiceDto.QuoteNumber,
                IncidentDetails = invoiceDto.IncidentDetails,
                CircuitAHighPressure = invoiceDto.CircuitAHighPressure,
                CircuitALowPressure = invoiceDto.CircuitALowPressure,
                CircuitAGT = invoiceDto.CircuitAGT,
                CircuitART = invoiceDto.CircuitART,
                CircuitAPower = invoiceDto.CircuitAPower,
                CircuitAAmpere = invoiceDto.CircuitAAmpere,
                CircuitBHighPressure = invoiceDto.CircuitBHighPressure,
                CircuitBLowPressure = invoiceDto.CircuitBLowPressure,
                CircuitBGT = invoiceDto.CircuitBGT,
                CircuitBRT = invoiceDto.CircuitBRT,
                CircuitBPower = invoiceDto.CircuitBPower,
                CircuitBAmpere = invoiceDto.CircuitBAmpere,
                VoucherNumber = invoiceDto.VoucherNumber,
                VoucherType = invoiceDto.VoucherType,
                VoucherExpiry = invoiceDto.VoucherExpiry,
                FinalNote = invoiceDto.FinalNote,
                Status = "Draft",
                SubTotal = 0,
                Tax = 0,
                Discount = 0,
                GrandTotal = 0,
                CreatedAt = DateTime.Now
            };

            _context.ProformaInvoices.Add(invoice);
            await _context.SaveChangesAsync();

            decimal subTotal = 0;
            decimal totalTax = 0;
            decimal totalDiscount = 0;

            if (invoiceDto.Items != null && invoiceDto.Items.Any())
            {
                foreach (var item in invoiceDto.Items)
                {
                    decimal lineTotal = ((item.UnitPrice ?? 0) * (item.Quantity ?? 1)) + (item.Tax ?? 0) - (item.Discount ?? 0);
                    
                    subTotal += (item.UnitPrice ?? 0) * (item.Quantity ?? 1);
                    totalTax += (item.Tax ?? 0);
                    totalDiscount += (item.Discount ?? 0);

                    _context.ProformaInvoiceItems.Add(new ProformaInvoiceItem
                    {
                        InvoiceID = invoice.InvoiceID,
                        Description = item.Description,
                        Quantity = item.Quantity,
                        Unit = item.Unit,
                        UnitPrice = item.UnitPrice,
                        Discount = item.Discount,
                        Tax = item.Tax,
                        TotalPrice = lineTotal
                    });
                }
                
                invoice.SubTotal = subTotal;
                invoice.Tax = totalTax;
                invoice.Discount = totalDiscount;
                invoice.GrandTotal = subTotal + totalTax - totalDiscount;

                await _context.SaveChangesAsync();
            }

            return Ok(new { Message = "Invoice created successfully", InvoiceID = invoice.InvoiceID });
        }

        // PUT: api/MasterCards/5/status
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            var masterCard = await _context.MasterCards.FindAsync(id);
            if (masterCard == null) return NotFound();

            masterCard.Status = status;
            masterCard.UpdatedAt = DateTime.Now;
            _context.Entry(masterCard).State = EntityState.Modified;
            
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    public class ProformaInvoiceDto
    {
        public string FIRNumber { get; set; }
        public string QuoteNumber { get; set; }
        public string IncidentDetails { get; set; }
        public decimal? CircuitAHighPressure { get; set; }
        public decimal? CircuitALowPressure { get; set; }
        public decimal? CircuitAGT { get; set; }
        public decimal? CircuitART { get; set; }
        public decimal? CircuitAPower { get; set; }
        public decimal? CircuitAAmpere { get; set; }
        public decimal? CircuitBHighPressure { get; set; }
        public decimal? CircuitBLowPressure { get; set; }
        public decimal? CircuitBGT { get; set; }
        public decimal? CircuitBRT { get; set; }
        public decimal? CircuitBPower { get; set; }
        public decimal? CircuitBAmpere { get; set; }
        public string VoucherNumber { get; set; }
        public string VoucherType { get; set; }
        public DateTime? VoucherExpiry { get; set; }
        public string FinalNote { get; set; }
        public List<InvoiceItemDto> Items { get; set; }
    }

    public class InvoiceItemDto
    {
        public string Description { get; set; }
        public int? Quantity { get; set; }
        public string Unit { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? Discount { get; set; }
        public decimal? Tax { get; set; }
    }
}
