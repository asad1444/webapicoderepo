using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartProManWebAPI.Data;
using SmartProManWebAPI.Models;

namespace SmartProManWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobMessagesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public JobMessagesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/JobMessages/5
        [HttpGet("{jobId}")]
        public async Task<ActionResult<IEnumerable<JobMessage>>> GetMessagesForJob(int jobId)
        {
            var messages = await _context.JobMessages
                .Where(m => m.JobID == jobId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            return Ok(messages);
        }

        // POST: api/JobMessages
        [HttpPost]
        public async Task<ActionResult<JobMessage>> PostMessage([FromBody] JobMessage message)
        {
            message.SentAt = DateTime.Now;
            
            _context.JobMessages.Add(message);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetMessagesForJob", new { jobId = message.JobID }, message);
        }
    }
}
