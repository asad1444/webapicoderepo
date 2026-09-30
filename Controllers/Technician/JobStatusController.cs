using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartProManWebAPI.Data;
using SmartProManWebAPI.Models;

namespace SmartProManWebAPI.Controllers.Technician
{
    /// <summary>
    /// Central job status + tracker update controller.
    /// Every action the technician takes updates BOTH:
    ///   - Jobs.Status
    ///   - MasterCards.Status
    ///   - JobTracking (step timestamps)
    /// So the company sees real-time updates in MasterCard.
    /// </summary>
    [Route("api/technician/job-status")]
    [ApiController]
    [AllowAnonymous]
    public class JobStatusController : ControllerBase
    {
        private readonly AppDbContext _context;

        public JobStatusController(AppDbContext context)
        {
            _context = context;
        }

        // ── GET tracker for a job ─────────────────────────────────────────────
        // GET: api/technician/job-status/{jobId}/tracker
        [HttpGet("{jobId}/tracker")]
        public async Task<IActionResult> GetTracker(int jobId)
        {
            var tracking = await _context.JobTracking
                .FirstOrDefaultAsync(t => t.JobID == jobId);

            var job = await _context.Jobs.FindAsync(jobId);
            var mc  = await _context.MasterCards
                .FirstOrDefaultAsync(m => m.JobID == jobId);

            if (job == null) return NotFound("Job not found.");

            // Build step list with timestamps and completion status
            var steps = BuildSteps(tracking, job, mc);

            return Ok(new
            {
                jobId,
                jobStatus  = job.Status,
                mcStatus   = mc?.Status,
                currentStep = tracking?.CurrentStep ?? "Assigned",
                steps,
                lastUpdated = tracking?.LastUpdated
            });
        }

        // ── STEP 1: On Way ────────────────────────────────────────────────────
        // POST: api/technician/job-status/{jobId}/on-way
        [HttpPost("{jobId}/on-way")]
        public async Task<IActionResult> SetOnWay(int jobId)
        {
            var (job, mc, tracking) = await Load(jobId);
            if (job == null) return NotFound();

            job.Status = "On Route";
            if (mc != null) { mc.Status = "On Way"; mc.UpdatedAt = DateTime.Now; }

            tracking.OnWayTime   = tracking.OnWayTime ?? DateTime.Now;
            tracking.CurrentStep = "On Way";
            tracking.LastUpdated = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(BuildResponse(job, mc, tracking));
        }

        // ── STEP 2: Arrived ───────────────────────────────────────────────────
        // POST: api/technician/job-status/{jobId}/arrived
        [HttpPost("{jobId}/arrived")]
        public async Task<IActionResult> SetArrived(int jobId)
        {
            var (job, mc, tracking) = await Load(jobId);
            if (job == null) return NotFound();

            job.Status = "Arrived";
            if (mc != null) { mc.Status = "Arrived"; mc.UpdatedAt = DateTime.Now; }

            tracking.CurrentStep = "Arrived";
            tracking.LastUpdated = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(BuildResponse(job, mc, tracking));
        }

        // ── STEP 3: Unit Registration Started ─────────────────────────────────
        // POST: api/technician/job-status/{jobId}/unit-reg-started
        [HttpPost("{jobId}/unit-reg-started")]
        public async Task<IActionResult> SetUnitRegStarted(int jobId)
        {
            var (job, mc, tracking) = await Load(jobId);
            if (job == null) return NotFound();

            job.Status = "In Progress";
            if (mc != null) { mc.Status = "In Progress"; mc.UpdatedAt = DateTime.Now; }

            tracking.CurrentStep = "Unit Registration";
            tracking.LastUpdated = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(BuildResponse(job, mc, tracking));
        }

        // ── STEP 4: Unit Registration Completed ───────────────────────────────
        // POST: api/technician/job-status/{jobId}/unit-reg-done
        [HttpPost("{jobId}/unit-reg-done")]
        public async Task<IActionResult> SetUnitRegDone(int jobId)
        {
            var (job, mc, tracking) = await Load(jobId);
            if (job == null) return NotFound();

            job.Status = "In Progress";
            if (mc != null) { mc.Status = "Unit Registered"; mc.UpdatedAt = DateTime.Now; }

            tracking.UnitRegistrationTime = tracking.UnitRegistrationTime ?? DateTime.Now;
            tracking.CurrentStep          = "FIR / Quote";
            tracking.LastUpdated          = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(BuildResponse(job, mc, tracking));
        }

        // ── STEP 5: Proforma / FIR Submitted ─────────────────────────────────
        // POST: api/technician/job-status/{jobId}/proforma-done
        [HttpPost("{jobId}/proforma-done")]
        public async Task<IActionResult> SetProformaDone(int jobId)
        {
            var (job, mc, tracking) = await Load(jobId);
            if (job == null) return NotFound();

            job.Status = "In Progress";
            if (mc != null) { mc.Status = "Quote Submitted"; mc.UpdatedAt = DateTime.Now; }

            tracking.FIRTime     = tracking.FIRTime     ?? DateTime.Now;
            tracking.QuoteTime   = tracking.QuoteTime   ?? DateTime.Now;
            tracking.CurrentStep = "Proforma Done";
            tracking.LastUpdated = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(BuildResponse(job, mc, tracking));
        }

        // ── STEP 5.5: Start Payment (Payment Collection In Progress) ────────────────
        [HttpPost("{jobId}/start-payment")]
        public async Task<IActionResult> StartPayment(int jobId)
        {
            var (job, mc, tracking) = await Load(jobId);
            if (job == null) return NotFound();

            // Opening the payment page does not mean payment was received.
            // Keep the tracker at its current step until SubmitPayment succeeds.
            return Ok(BuildResponse(job, mc, tracking));
        }

        // ── STEP 6: Job Completed ─────────────────────────────────────────────
        // POST: api/technician/job-status/{jobId}/complete
        [HttpPost("{jobId}/complete")]
        public async Task<IActionResult> SetComplete(int jobId)
        {
            var (job, mc, tracking) = await Load(jobId);
            if (job == null) return NotFound();

            job.Status       = "Completed";
            job.CompletedAt  = DateTime.Now;
            if (mc != null) { mc.Status = "Completed"; mc.UpdatedAt = DateTime.Now; }

            tracking.JobClosureTime = tracking.JobClosureTime ?? DateTime.Now;
            tracking.CurrentStep    = "Completed";
            tracking.LastUpdated    = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(BuildResponse(job, mc, tracking));
        }

        // ── PAYMENT ───────────────────────────────────────────────────────────
        // POST: api/technician/jobs/{jobId}/payment (mapped here as /api/technician/job-status/{jobId}/payment to keep it simple, wait, flutter calls api/technician/jobs/{jobId}/payment)
        // I will add [Route("/api/technician/jobs/{jobId}/payment")] specifically for this.
        [HttpPost("/api/technician/jobs/{jobId}/payment")]
        public async Task<IActionResult> SubmitPayment(int jobId, [FromBody] PaymentRequestDto dto)
        {
            var (job, mc, tracking) = await Load(jobId);
            if (job == null) return NotFound("Job not found.");

            // Find the active invoice for this job
            var invoice = await _context.Invoices.OrderByDescending(i => i.Id).FirstOrDefaultAsync(i => i.JobID == jobId);
            
            if (invoice == null) 
            {
                if (mc == null) return BadRequest("No MasterCard found for this job.");
                
                var proforma = await _context.ProformaInvoices.FirstOrDefaultAsync(p => p.MasterCardID == mc.MasterCardID);
                if (proforma == null) return BadRequest("No invoice found for this job.");

                // Auto-create the final Invoice from Proforma
                invoice = new Invoice
                {
                    JobID = jobId,
                    InvoiceNumber = "INV-" + Guid.NewGuid().ToString().Substring(0, 6).ToUpper(),
                    InvoiceDate = DateTime.Now,
                    GrandTotal = proforma.GrandTotal ?? 0
                };
                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();
            }

            // Create Payment
            var payment = new Payment
            {
                InvoiceId = invoice.Id,
                PaymentMethod = dto.Method?.ToLower() == "online" ? SmartProManWebAPI.Models.Enums.PaymentMethod.Online : SmartProManWebAPI.Models.Enums.PaymentMethod.Cash,
                AmountReceived = dto.Amount,
                TransactionReference = dto.Method?.ToLower() == "online" ? "ONL-" + Guid.NewGuid().ToString().Substring(0, 8) : "CASH",
                IsSuccessful = true,
                PaymentDate = DateTime.Now
            };

            _context.Payments.Add(payment);

            // Payment completion advances only Payment Collection.
            // Job Closure is performed separately by the explicit Close Job action.
            job.Status = "In Progress";
            job.CompletedAt = null;
            if (mc != null) { mc.Status = "Payment Collected"; mc.UpdatedAt = DateTime.Now; }

            tracking.FCRTime = tracking.FCRTime ?? DateTime.Now;
            tracking.CurrentStep = "Payment Collection";
            tracking.LastUpdated = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Payment recorded successfully", paymentId = payment.Id });
        }

        public class PaymentRequestDto
        {
            public string? Method { get; set; }
            public decimal Amount { get; set; }
            public string? ReceivedBy { get; set; }
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private async Task<(Job? job, MasterCard? mc, JobTracking tracking)> Load(int jobId)
        {
            var job      = await _context.Jobs.FindAsync(jobId);
            var mc       = await _context.MasterCards.FirstOrDefaultAsync(m => m.JobID == jobId);
            var tracking = await _context.JobTracking.FirstOrDefaultAsync(t => t.JobID == jobId);

            // Auto-create tracking record if missing
            if (tracking == null && job != null)
            {
                tracking = new JobTracking
                {
                    JobID       = jobId,
                    CurrentStep = "Assigned",
                    ETA         = "Pending",
                    LastUpdated = DateTime.Now,
                };
                _context.JobTracking.Add(tracking);
                await _context.SaveChangesAsync();
            }

            return (job, mc, tracking!);
        }

        private static object BuildResponse(Job job, MasterCard? mc, JobTracking tracking) => new
        {
            jobStatus   = job.Status,
            mcStatus    = mc?.Status,
            currentStep = tracking.CurrentStep,
            lastUpdated = tracking.LastUpdated,
            message     = "Status updated successfully."
        };

        private static List<object> BuildSteps(JobTracking? t, Job job, MasterCard? mc)
        {
            var current = t?.CurrentStep ?? "Assigned";

            var steps = new List<(string key, string label, DateTime? time)>
            {
                ("Assigned",           "Assigned",              job.AssignedAt ?? job.CreatedAt),
                ("On Way",             "On Way",                t?.OnWayTime),
                ("Unit Registration",  "Unit Registration",     t?.UnitRegistrationTime),
                ("FIR / Quote",        "FIR / Quote",           t?.FIRTime ?? t?.QuoteTime),
                    ("Payment Collection", "Payment Collection",    t?.FCRTime ?? t?.JobStatusTime),
                    ("Job Closure",        "Job Closure",           t?.JobClosureTime ?? job.CompletedAt),
            };

            var stepOrder = steps.Select(s => s.key).ToList();
            var currentIdx = stepOrder.IndexOf(current);

            bool isArrived = current == "Arrived";
            if (isArrived) currentIdx = 2;
            if (current == "Assigned") currentIdx = 1;
            
            bool isProformaDone = current == "Proforma Done";
            if (isProformaDone) currentIdx = 4;
            if (current == "Payment Collection") currentIdx = steps.Count - 1;
            if (current == "Completed") currentIdx = steps.Count;

            return steps.Select((s, i) => (object)new
            {
                key       = s.key,
                label     = s.label,
                time      = s.time?.ToString("dd MMM yyyy hh:mm tt"),
                completed = i < currentIdx || (s.key == "Job Closure" && job.Status == "Completed"),
                active    = i == currentIdx && job.Status != "Completed",
            }).ToList();
        }
    }
}
