using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartProManWebAPI.Data;

namespace SmartProManWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobStuckHistoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public JobStuckHistoryController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/JobStuckHistory
        // Get all stuck job records (admin review panel)
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var query = _context.JobStuckHistories
                .Include(s => s.Job)
                    .ThenInclude(j => j.Client)
                .Include(s => s.Job)
                    .ThenInclude(j => j.Technician)
                .AsQueryable();

            if (from.HasValue)
                query = query.Where(s => s.CreatedAt >= from.Value);

            if (to.HasValue)
                query = query.Where(s => s.CreatedAt <= to.Value.AddDays(1));

            var records = await query
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new
                {
                    s.Id,
                    s.JobID,
                    CRNumber = s.Job.CRNumber,
                    ClientName = s.Job.Client.ClientName,
                    TechnicianName = s.Job.Technician != null ? s.Job.Technician.FullName : "Unassigned",
                    s.StuckReason,
                    s.StuckRemarks,
                    s.GpsLocation,
                    s.CreatedAt,
                    JobStatus = s.Job.Status
                })
                .ToListAsync();

            return Ok(records);
        }

        // GET: api/JobStuckHistory/job/{jobId}
        // Get stuck history for a specific job
        [HttpGet("job/{jobId}")]
        public async Task<IActionResult> GetByJob(int jobId)
        {
            var history = await _context.JobStuckHistories
                .Where(s => s.JobID == jobId)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new
                {
                    s.Id,
                    s.StuckReason,
                    s.StuckRemarks,
                    s.GpsLocation,
                    s.CreatedAt
                })
                .ToListAsync();

            return Ok(history);
        }

        // PUT: api/JobStuckHistory/{id}/resolve
        // Admin resolves a stuck job
        [HttpPut("{id}/resolve")]
        public async Task<IActionResult> Resolve(int id, [FromBody] string resolutionNote)
        {
            var stuck = await _context.JobStuckHistories
                .Include(s => s.Job)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (stuck == null) return NotFound("Stuck record not found.");

            // Update the job status back to In Progress
            stuck.Job.Status = "In Progress";
            stuck.StuckRemarks = $"{stuck.StuckRemarks} | RESOLVED: {resolutionNote}";

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Job unstuck and set back to In Progress." });
        }
    }
}
