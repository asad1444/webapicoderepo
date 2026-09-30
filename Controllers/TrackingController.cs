using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartProManWebAPI.Data;
using SmartProManWebAPI.Models;

namespace SmartProManWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TrackingController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TrackingController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/Tracking/location
        [HttpPost("location")]
        public async Task<IActionResult> PostLocation([FromBody] TechnicianLocation location)
        {
            location.RecordedAt = DateTime.Now;
            _context.TechnicianLocations.Add(location);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // PUT: api/Tracking/job/5/status
        [HttpPut("job/{jobId}/status")]
        public async Task<IActionResult> UpdateJobTrackingStatus(int jobId, [FromBody] JobTrackingUpdateDto dto)
        {
            var tracking = await _context.JobTracking.FirstOrDefaultAsync(t => t.JobID == jobId);
            if (tracking == null)
            {
                tracking = new JobTracking { JobID = jobId };
                _context.JobTracking.Add(tracking);
            }

            var now = DateTime.Now;
            var normalizedStep = NormalizeStep(dto.Step);
            tracking.CurrentStep = normalizedStep;
            tracking.LastUpdated = now;
            if (!string.IsNullOrEmpty(dto.ETA)) tracking.ETA = dto.ETA;

            switch (normalizedStep.ToLowerInvariant())
            {
                case "onway": tracking.OnWayTime ??= now; break;
                case "unitregistration": tracking.UnitRegistrationTime ??= now; break;
                case "fir": tracking.FIRTime ??= now; break;
                case "quote": tracking.QuoteTime ??= now; break;
                case "fcr": tracking.FCRTime ??= now; break;
                case "completed": tracking.JobClosureTime ??= now; break;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        private static string NormalizeStep(string? step)
        {
            if (string.IsNullOrWhiteSpace(step)) return "Assigned";

            var value = step.Trim();
            return value.ToLowerInvariant() switch
            {
                "assigned" or "jobassigned" or "job_assigned" => "Assigned",
                "onway" or "on_way" or "on way" => "OnWay",
                "registration" or "unitregistration" or "unit_registration" or "unit reg" => "UnitRegistration",
                "fir" or "proforma" or "pro forma" => "FIR",
                "quote" or "quoteinvoice" or "quote_invoice" => "Quote",
                "fcr" or "paymentcollected" or "payment_collected" => "FCR",
                "closure" or "completed" or "jobclosed" or "job_closed" => "Completed",
                _ => value
            };
        }
    }

    public class JobTrackingUpdateDto
    {
        public string Step { get; set; }
        public string? ETA { get; set; }
    }
}
