using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartProManWebAPI.Data;
using SmartProManWebAPI.Models;

namespace SmartProManWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RequestsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RequestsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Requests
        [HttpGet]
        public async Task<IActionResult> GetRequests()
        {
            var requests = await _context.Requests
                .Include(r => r.Job)
                    .ThenInclude(j => j.Technician)
                .OrderByDescending(r => r.RequestID)
                .ToListAsync();

            var result = requests.Select(r => new
            {
                r.RequestID,
                r.ClientRequestNumber,
                r.Time,
                r.Date,
                r.ClientName,
                r.ClientContact,
                r.ClientRequest,
                r.KindOf,
                r.Category,
                r.Zone,
                r.Status,
                r.ClientStatus,
                r.CompanyID,
                r.JobID,
                TechnicianName = r.Job != null && r.Job.Technician != null
                    ? r.Job.Technician.FullName
                    : null
            });

            return Ok(result);
        }

        // GET: api/Requests/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Request>> GetRequest(int id)
        {
            var request = await _context.Requests.FindAsync(id);

            if (request == null)
                return NotFound();

            return request;
        }

        // POST: api/Requests
        [HttpPost]
        public async Task<ActionResult<Request>> CreateRequest([FromBody] RequestCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Auto-generate ClientRequestNumber: REQ-YYYYMMDD-XXXX
            var today = DateTime.Now;
            var countToday = await _context.Requests
                .CountAsync(r => r.Date.Date == today.Date);

            var serial = (countToday + 1).ToString("D4"); // e.g. 0001
            var clientRequestNumber = $"REQ-{today:yyyyMMdd}-{serial}";

            var request = new Request
            {
                ClientRequestNumber  = clientRequestNumber,
                Date                 = today.Date,
                Time                 = today,
                ClientName           = dto.ClientName,
                ClientContact        = dto.ClientContact,
                ClientRequest        = dto.ClientRequest,
                KindOf               = dto.KindOf,
                Category             = dto.Category,
                Zone                 = dto.Zone,
                ClientStatus         = "Active",
                SpecialInstructions  = dto.SpecialInstructions,
                ServiceAddress       = dto.ServiceAddress,
                Priority             = dto.Priority ?? "Normal"
            };

            _context.Requests.Add(request);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRequest), new { id = request.RequestID }, request);
        }

        // PUT: api/Requests/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRequest(int id, [FromBody] RequestCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var request = await _context.Requests.FindAsync(id);
            if (request == null)
                return NotFound();

            request.ClientName    = dto.ClientName;
            request.ClientContact = dto.ClientContact;
            request.ClientRequest = dto.ClientRequest;
            request.KindOf        = dto.KindOf;
            request.Category      = dto.Category;
            request.Zone          = dto.Zone;

            _context.Entry(request).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/Requests/5/status
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            if (status != "Active" && status != "Suspend")
                return BadRequest("Invalid status. Must be 'Active' or 'Suspend'.");

            var request = await _context.Requests.FindAsync(id);
            if (request == null) return NotFound();

            request.ClientStatus = status == "Suspend" ? "Suspended" : "Active";
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/Requests/5/assign-foreman
        [HttpPost("{id}/assign-foreman")]
        public async Task<IActionResult> AssignForeman(int id, [FromBody] int technicianId)
        {
            var request = await _context.Requests
                .Include(r => r.Job)
                .FirstOrDefaultAsync(r => r.RequestID == id);
            if (request == null) return NotFound(new { message = "Request not found." });
            if (request.Job != null || request.Status == "Assigned")
                return BadRequest(new { message = "This request has already been assigned." });

            var foreman = await _context.Technicians
                .FirstOrDefaultAsync(t => t.TechnicianID == technicianId && t.Designation == "Foreman");
            if (foreman == null)
                return BadRequest(new { message = "Foreman not found." });
            if (foreman.ApprovalStatus == "Suspended")
                return BadRequest(new { message = "Suspended foreman cannot receive jobs. Activate the foreman first." });

            var job = new Job
            {
                RequestID = request.RequestID,
                CompanyID = request.CompanyID,
                TechnicianID = foreman.TechnicianID,
                AssignmentSource = "Foreman",
                CRNumber = request.ClientRequestNumber,
                CRID = $"CRID-{request.RequestID:D6}",
                ClientRequest = request.ClientRequest,
                IssueDescription = request.KindOf,
                Category = request.Category,
                Status = "Assigned",
                CreatedAt = DateTime.Now,
                AssignedAt = DateTime.Now,
                BidOpenedAt = request.BidOpenedAt,
                BroadcastZoneLevel = request.BroadcastZoneLevel
            };

            _context.Jobs.Add(job);
            request.Status = "Assigned";
            await _context.SaveChangesAsync();
            request.JobID = job.JobID;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Job assigned to foreman successfully.", jobID = job.JobID });
        }

        // DELETE: api/Requests/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRequest(int id)
        {
            var request = await _context.Requests.FindAsync(id);
            if (request == null)
                return NotFound();

            _context.Requests.Remove(request);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Requests/{crn}/award
        // Award a request to a company based on ClientRequestNumber
        [HttpPost("{crn}/award")]
        public async Task<IActionResult> AwardRequest(string crn, [FromBody] AwardRequestDto dto)
        {
            if (string.IsNullOrEmpty(crn) || dto == null)
                return BadRequest(new { message = "Invalid request number or award data" });

            var request = await _context.Requests.FirstOrDefaultAsync(r => r.ClientRequestNumber == crn);
            if (request == null)
                return NotFound(new { message = $"Request with CRN {crn} not found" });

            if (request.Status != "Open")
                return BadRequest(new { message = "Request is not in Open status" });

            // Award to company
            request.CompanyID = dto.CompanyId;
            request.Status = "Awarded";
            request.Zone = dto.Zone ?? request.Zone; // Update zone if provided

            _context.Entry(request).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Request awarded successfully",
                requestId = request.RequestID,
                companyId = request.CompanyID,
                zone = request.Zone
            });
        }
    }

    // DTO - sirf form se ye fields aayenge
    public class RequestCreateDto
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string ClientName { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public string ClientContact { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public string ClientRequest { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public string KindOf { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public string Category { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public string Zone { get; set; }

        public string? SpecialInstructions { get; set; }
        public string? ServiceAddress { get; set; }
        public string? Priority { get; set; } = "Normal";
    }

    // DTO for awarding a request to a company
    public class AwardRequestDto
    {
        [System.ComponentModel.DataAnnotations.Required]
        public int CompanyId { get; set; }

        public string? CompanyName { get; set; }

        public string? Zone { get; set; }
    }
}
