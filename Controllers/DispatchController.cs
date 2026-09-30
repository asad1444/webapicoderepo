using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartProManWebAPI.Data;
using SmartProManWebAPI.Models;

namespace SmartProManWebAPI.Controllers
{
 
    [Route("api/[controller]")]
    [ApiController]
    public class DispatchController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DispatchController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Dispatch/tickets
        [HttpGet("tickets")]
        public async Task<IActionResult> GetTickets()
        {
            // Grid: Time, Client Request (Asset Name), Kind of (Issue), CRN, CRID, Category, Status
            var tickets = await _context.Jobs
                .OrderByDescending(j => j.CreatedAt)
                .Select(j => new
                {
                    Time = j.CreatedAt,
                    ClientRequest = j.ClientRequest,
                    KindOf = j.IssueDescription,
                    CRN = j.CRNumber,
                    CRID = j.CRID,
                    Category = j.Category,
                    Status = j.Status,
                    j.JobID
                }).ToListAsync();

            return Ok(tickets);
        }

        // POST: api/Dispatch/tickets
        [HttpPost("tickets")]
        public async Task<IActionResult> CreateTicket([FromBody] Job jobDto)
        {
            // Auto CRN generator logic
            var count = await _context.Jobs.CountAsync();
            jobDto.CRNumber = $"CRN-{DateTime.Now:yyyyMMdd}-{count + 1}";
            jobDto.Status = "Open";
            jobDto.CreatedAt = DateTime.Now;

            _context.Jobs.Add(jobDto);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTickets", new { id = jobDto.JobID }, jobDto);
        }

        // GET: api/Dispatch/6/matching-companies
        [HttpGet("{jobId}/matching-companies")]
        public async Task<IActionResult> GetMatchingCompaniesForDispatch(int jobId)
        {
            var job = await _context.Jobs.Include(j => j.Client).FirstOrDefaultAsync(j => j.JobID == jobId);
            if (job == null) return NotFound();

            var clientZone = job.Client.Zone;
            
            // Intelligent match: find companies in client Zone that are Active
            var companies = await _context.Companies
                .Where(c => c.Zone == clientZone && c.Status == "Active")
                .Select(c => new
                {
                    c.CompanyID,
                    c.CompanyName,
                    c.ServiceType,
                    c.InchargePhone
                }).ToListAsync();

            return Ok(new { JobZone = clientZone, AvailableCompanies = companies });
        }

        // POST: api/Dispatch/bids
        [HttpPost("bids")]
        public async Task<IActionResult> SubmitBid([FromBody] DispatchBid bid)
        {
            bid.BidTime = DateTime.Now;
            bid.Status = "Pending";
            
            // 15 minute protocol validation logic could be enforced here checking (bid.BidTime - Job.CreatedAt)
            
            _context.DispatchBids.Add(bid);
            await _context.SaveChangesAsync();

            return Ok(bid);
        }

        // POST: api/Dispatch/6/award
        [HttpPost("{jobId}/award")]
        public async Task<IActionResult> AwardJob(int jobId, [FromBody] int winnerCompanyId)
        {
            var job = await _context.Jobs.FindAsync(jobId);
            if (job == null) return NotFound();

            // Find best bid logic
            var winningBid = await _context.DispatchBids
                .Where(b => b.JobID == jobId && b.CompanyID == winnerCompanyId)
                .FirstOrDefaultAsync();

            if (winningBid != null)
            {
                winningBid.IsWinner = true;
                winningBid.Status = "Accepted";
            }

            job.CompanyID = winnerCompanyId;
            job.Status = "Dispatched";
            job.AssignedAt = DateTime.Now;
            
            _context.Entry(job).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Job awarded successfully", JobID = jobId, CompanyID = winnerCompanyId });
        }
    }
}
