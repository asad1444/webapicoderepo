using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartProManWebAPI.Data;
using SmartProManWebAPI.Models;

namespace SmartProManWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UnitRegistrationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UnitRegistrationsController(AppDbContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: api/UnitRegistrations/auto-data/job/{jobId}
        // Returns auto-fill data from Job + Request table
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("auto-data/job/{jobId}")]
        public async Task<IActionResult> GetAutoDataByJob(int jobId)
        {
            var job = await _context.Jobs
                .Include(j => j.Request)
                .Include(j => j.Client)
                .FirstOrDefaultAsync(j => j.JobID == jobId);

            if (job == null) return NotFound("Job not found.");

            var req    = job.Request;
            var client = job.Client;

            return Ok(new
            {
                currentDate    = DateTime.Now.ToString("dd MMM yyyy"),
                mobileNumber   = req?.ClientContact ?? client?.Phone ?? "",
                homeAddress    = req?.ServiceAddress ?? client?.Home ?? "",
                zone           = req?.Zone ?? client?.Zone ?? "",
                area           = client?.Area ?? "",
                street         = client?.Street ?? "",
                floor          = client?.Floor ?? "",
                floorZone      = client?.FloorZone ?? "",
                topZone        = client?.TopZone ?? "",
                // Service name from request — what the customer asked for
                serviceName    = req?.ClientRequest ?? job.ClientRequest ?? "",
                serviceType    = req?.Category ?? job.Category ?? "",
                clientName     = req?.ClientName ?? client?.ClientName ?? "",
                requestId      = job.RequestID,
                jobId          = job.JobID,
                technicianId   = job.TechnicianID,
                clientId       = job.ClientID ?? 0,
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: api/UnitRegistrations/auto-data/{clientId}  (legacy)
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("auto-data/{clientId}")]
        public async Task<IActionResult> GetAutoData(int clientId)
        {
            var client = await _context.Clients.FindAsync(clientId);
            if (client == null) return NotFound();
            return Ok(new
            {
                CurrentDate  = DateTime.Now,
                MobileNumber = client.Phone,
                client.Home,
                client.ServeLocation,
                client.Zone,
                client.Area,
                client.Street,
                client.Floor,
                client.FloorZone,
                client.TopZone
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: api/UnitRegistrations/{id}
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetUnitRegistration(int id)
        {
            var unit = await _context.UnitRegistrations
                .Include(u => u.Client)
                .FirstOrDefaultAsync(u => u.UnitID == id);

            if (unit == null) return NotFound();

            var media = await _context.UnitRegistrationMedia
                .Where(m => m.UnitID == id)
                .Select(m => new
                {
                    m.MediaID,
                    m.MediaType,
                    m.Category,
                    m.FilePath,
                    m.UploadedAt
                })
                .ToListAsync();

            return Ok(new
            {
                unit.UnitID,
                unit.JobID,
                unit.TechnicianID,
                unit.ClientID,
                unit.RegistrationDate,
                unit.ModelNumber,
                unit.SerialNumber,
                unit.ServiceName,
                unit.Floor,
                unit.FloorZone,
                unit.TopZone,
                unit.QrCodeData,
                unit.AdminRemarks,
                ClientMobileNumber = unit.Client?.Phone,
                Media = media
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: api/UnitRegistrations/by-job/{jobId}
        // Get unit registration for a specific job (for company frontend)
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("by-job/{jobId}")]
        public async Task<IActionResult> GetByJob(int jobId)
        {
            var unit = await _context.UnitRegistrations
                .Include(u => u.Client)
                .FirstOrDefaultAsync(u => u.JobID == jobId);

            if (unit == null) return NotFound("No unit registration for this job.");

            var job = await _context.Jobs
                .Include(j => j.Request)
                .FirstOrDefaultAsync(j => j.JobID == jobId);

            var mediaList = await _context.UnitRegistrationMedia
                .Where(m => m.UnitID == unit.UnitID)
                .Select(m => new { m.MediaID, m.MediaType, m.Category, m.UploadedAt })
                .ToListAsync();

            var mediaUrlBase = $"{Request.Scheme}://{Request.Host}/api/UnitRegistrations/media/";
            var media = mediaList.Select(m => new {
                m.MediaID,
                m.MediaType,
                m.Category,
                FilePath = mediaUrlBase + m.MediaID,
                m.UploadedAt
            }).ToList();

            return Ok(new
            {
                unit.UnitID,
                unit.JobID,
                unit.TechnicianID,
                unit.ClientID,
                unit.RegistrationDate,
                unit.ModelNumber,
                unit.SerialNumber,
                unit.ServiceName,
                unit.Zone,
                unit.Area,
                unit.Street,
                unit.Floor,
                unit.FloorZone,
                unit.TopZone,
                unit.QrCodeData,
                unit.AdminRemarks,
                ClientMobileNumber = job?.Request?.ClientContact ?? unit.Client?.Phone,
                ClientArea = job?.Request?.ServiceAddress ?? unit.Client?.Home,
                ClientZone = job?.Request?.Zone ?? unit.Client?.Zone,
                Images = media.Where(m => m.MediaType == "image").ToList(),
                Videos = media.Where(m => m.MediaType == "video").ToList()
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: api/UnitRegistrations/media/{mediaId}
        // Streams media content instead of returning massive Base64 strings in JSON
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("media/{mediaId}")]
        public async Task<IActionResult> GetMedia(int mediaId)
        {
            var media = await _context.UnitRegistrationMedia
                .Where(m => m.MediaID == mediaId)
                .Select(m => m.FilePath)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(media)) return NotFound();

            var parts = media.Split(',');
            if (parts.Length != 2) return BadRequest("Invalid media format");

            var meta = parts[0];
            var base64 = parts[1];

            var contentType = "application/octet-stream";
            if (meta.StartsWith("data:") && meta.Contains(";"))
            {
                contentType = meta.Substring(5, meta.IndexOf(';') - 5);
            }

            var bytes = Convert.FromBase64String(base64);
            return File(bytes, contentType, enableRangeProcessing: true);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: api/UnitRegistrations/qr/{jobId}
        // Get QR code data for a job (for scanning next time)
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("qr/{jobId}")]
        public async Task<IActionResult> GetQrData(int jobId)
        {
            var unit = await _context.UnitRegistrations
                .FirstOrDefaultAsync(u => u.JobID == jobId);

            if (unit == null || string.IsNullOrEmpty(unit.QrCodeData))
                return NotFound("No QR data found.");

            return Ok(new { qrData = unit.QrCodeData });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: api/UnitRegistrations/search
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string? companyName,
            [FromQuery] DateTime? date)
        {
            var query = _context.UnitRegistrations
                .Include(u => u.Technician)
                    .ThenInclude(t => t.Company)
                .AsQueryable();

            if (!string.IsNullOrEmpty(companyName))
                query = query.Where(u => u.Technician.Company.CompanyName.Contains(companyName));

            if (date.HasValue)
                query = query.Where(u => u.RegistrationDate.HasValue &&
                                         u.RegistrationDate.Value.Date == date.Value.Date);

            var results = await query
                .Select(u => new
                {
                    u.UnitID,
                    JobID          = u.JobID,
                    CompanyID      = u.Technician.CompanyID,
                    CompanyName    = u.Technician.Company.CompanyName,
                    u.TechnicianID,
                    TechnicianName = u.Technician.FullName,
                    u.RegistrationDate,
                    u.ModelNumber,
                    u.SerialNumber,
                    u.ServiceName,
                    u.QrCodeData,
                    CurrentStep = _context.JobTracking
                        .Where(t => t.JobID == u.JobID)
                        .Select(t => t.CurrentStep)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var mapped = results.Select(r =>
            {
                var currentStep = r.CurrentStep ?? string.Empty;
                var status = "Pending";

                if (r.RegistrationDate.HasValue)
                {
                    status = "Completed";
                }
                else if (!string.IsNullOrWhiteSpace(currentStep) &&
                         (currentStep.Equals("UnitRegistration", StringComparison.OrdinalIgnoreCase) ||
                          currentStep.Equals("OnWay", StringComparison.OrdinalIgnoreCase) ||
                          currentStep.Equals("FIR", StringComparison.OrdinalIgnoreCase) ||
                          currentStep.Equals("Quote", StringComparison.OrdinalIgnoreCase) ||
                          currentStep.Equals("FCR", StringComparison.OrdinalIgnoreCase)))
                {
                    status = "In Progress";
                }

                return new
                {
                    r.UnitID,
                    r.JobID,
                    r.CompanyID,
                    r.CompanyName,
                    r.TechnicianID,
                    r.TechnicianName,
                    r.RegistrationDate,
                    r.ModelNumber,
                    r.SerialNumber,
                    r.ServiceName,
                    r.QrCodeData,
                    Status = status
                };
            }).ToList();

            return Ok(mapped);
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST: api/UnitRegistrations
        // Save registration + media + generate QR code
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<ActionResult<object>> PostUnitRegistration(
            [FromBody] UnitRegistrationDto dto)
        {
            var existingUnit = await _context.UnitRegistrations
                .FirstOrDefaultAsync(u => u.JobID == dto.JobID);

            if (existingUnit != null)
            {
                if (string.IsNullOrWhiteSpace(existingUnit.QrCodeData))
                {
                    var existingJob = await _context.Jobs
                        .Include(j => j.Request)
                        .Include(j => j.Client)
                        .FirstOrDefaultAsync(j => j.JobID == existingUnit.JobID);

                    existingUnit.QrCodeData = JsonSerializer.Serialize(new
                    {
                        unitId = existingUnit.UnitID,
                        jobId = existingUnit.JobID,
                        modelNumber = existingUnit.ModelNumber,
                        serialNumber = existingUnit.SerialNumber,
                        serviceName = existingUnit.ServiceName,
                        clientName = existingJob?.Request?.ClientName ?? existingJob?.Client?.ClientName ?? "",
                        mobileNumber = existingJob?.Request?.ClientContact ?? existingJob?.Client?.Phone ?? "",
                        address = existingJob?.Request?.ServiceAddress ?? existingJob?.Client?.Home ?? "",
                        zone = existingJob?.Request?.Zone ?? existingJob?.Client?.Zone ?? "",
                        floor = existingUnit.Floor ?? "",
                        floorZone = existingUnit.FloorZone ?? "",
                        registeredAt = existingUnit.RegistrationDate?.ToString("yyyy-MM-dd HH:mm") ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                    });
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    message = "This Registration is already done",
                    alreadyRegistered = true,
                    unitId = existingUnit.UnitID,
                    qrCodeData = existingUnit.QrCodeData
                });
            }

            // Validate max 5 images
            var images = dto.Media?.Where(m => m.MediaType == "image").ToList() ?? new();
            if (images.Count > 5)
                return BadRequest("Maximum 5 images allowed.");

            var unit = new UnitRegistration
            {
                JobID            = dto.JobID,
                TechnicianID     = dto.TechnicianID,
                ClientID         = dto.ClientID > 0 ? dto.ClientID : null,
                ModelNumber      = dto.ModelNumber ?? "",
                SerialNumber     = dto.SerialNumber ?? "",
                ServiceName      = dto.ServiceName,
                Zone             = dto.Zone,
                Area             = dto.Area,
                Street           = dto.Street,
                Floor            = dto.Floor,
                FloorZone        = dto.FloorZone,
                TopZone          = dto.TopZone,
                AdminRemarks     = dto.AdminRemarks,
                RegistrationDate = DateTime.Now,
            };

            _context.UnitRegistrations.Add(unit);
            await _context.SaveChangesAsync();

            // Save media (images + videos)
            if (dto.Media != null && dto.Media.Any())
            {
                foreach (var m in dto.Media)
                {
                    _context.UnitRegistrationMedia.Add(new UnitRegistrationMedia
                    {
                        UnitID    = unit.UnitID,
                        MediaType = m.MediaType ?? "image",
                        Category  = m.Category  ?? dto.ServiceName ?? "General",
                        FilePath  = m.FilePath  ?? "",
                    });
                }
                await _context.SaveChangesAsync();
            }

            // ── Generate QR code data ─────────────────────────────────────
            // Get job + request info for QR
            var job = await _context.Jobs
                .Include(j => j.Request)
                .Include(j => j.Client)
                .FirstOrDefaultAsync(j => j.JobID == dto.JobID);

            var qrPayload = new
            {
                unitId      = unit.UnitID,
                jobId       = unit.JobID,
                modelNumber = unit.ModelNumber,
                serialNumber = unit.SerialNumber,
                serviceName  = unit.ServiceName,
                clientName   = job?.Request?.ClientName ?? job?.Client?.ClientName ?? "",
                mobileNumber = job?.Request?.ClientContact ?? job?.Client?.Phone ?? "",
                address      = job?.Request?.ServiceAddress ?? job?.Client?.Home ?? "",
                zone         = job?.Request?.Zone ?? job?.Client?.Zone ?? "",
                floor        = unit.Floor ?? "",
                floorZone    = unit.FloorZone ?? "",
                registeredAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            };

            unit.QrCodeData = JsonSerializer.Serialize(qrPayload);

            var tracking = await _context.JobTracking.FirstOrDefaultAsync(t => t.JobID == dto.JobID);
            if (tracking == null)
            {
                tracking = new JobTracking { JobID = dto.JobID, ETA = "" };
                _context.JobTracking.Add(tracking);
            }

            tracking.ETA ??= "";
            tracking.CurrentStep = "UnitRegistration";
            tracking.UnitRegistrationTime ??= DateTime.Now;
            tracking.LastUpdated = DateTime.Now;
            if (!tracking.OnWayTime.HasValue)
            {
                tracking.OnWayTime = tracking.UnitRegistrationTime;
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUnitRegistration), new { id = unit.UnitID }, new
            {
                unit.UnitID,
                unit.JobID,
                unit.ModelNumber,
                unit.SerialNumber,
                unit.ServiceName,
                unit.QrCodeData,
                unit.RegistrationDate,
                message = "Unit registered successfully."
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUT: api/UnitRegistrations/{id}/remarks
        // ─────────────────────────────────────────────────────────────────────
        [HttpPut("{id}/remarks")]
        public async Task<IActionResult> UpdateRemarks(int id, [FromBody] string adminRemarks)
        {
            var unit = await _context.UnitRegistrations.FindAsync(id);
            if (unit == null) return NotFound();
            unit.AdminRemarks = adminRemarks;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    // ── DTOs ──────────────────────────────────────────────────────────────────
    public class UnitRegistrationDto
    {
        public int    JobID        { get; set; }
        public int    TechnicianID { get; set; }
        public int?   ClientID     { get; set; }
        public string? ModelNumber  { get; set; }
        public string? SerialNumber { get; set; }
        public string? ServiceName  { get; set; }
        public string? Zone         { get; set; }
        public string? Area         { get; set; }
        public string? Street       { get; set; }
        public string? Floor        { get; set; }
        public string? FloorZone    { get; set; }
        public string? TopZone      { get; set; }
        public string? AdminRemarks { get; set; }
        public List<MediaDto>? Media { get; set; }
    }

    public class MediaDto
    {
        public string? MediaType { get; set; }  // "image" or "video"
        public string? Category  { get; set; }  // service name
        public string? FilePath  { get; set; }  // base64 data URL
    }
}
