using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartProManWebAPI.Data;
using SmartProManWebAPI.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace SmartProManWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PayFastController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public PayFastController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // POST: api/PayFast/initiate
        [HttpPost("initiate")]
        public async Task<IActionResult> InitiatePayment([FromBody] InitiatePaymentRequest req)
        {
            if (req.JobId <= 0 || req.Amount <= 0)
                return BadRequest("Invalid JobId or Amount.");

            var job = await _context.Jobs.FindAsync(req.JobId);
            if (job == null)
                return NotFound("Job not found.");

            // Create OrderId
            var orderId = $"SPM-JOB-{req.JobId}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

            var transaction = new OnlinePaymentTransaction
            {
                JobId = req.JobId,
                OrderId = orderId,
                Amount = req.Amount,
                Status = "Pending",
                CreatedAt = DateTime.Now,
                GatewayTransactionId = "", // Provide empty string to avoid NOT NULL DB constraint error
                PaymentMethod = ""         // Provide empty string to avoid NOT NULL DB constraint error
            };

            _context.OnlinePaymentTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            // Prepare PayFast Token Generation / Redirection Data
            var merchantId = _config["PayFast:MerchantId"];
            var securedKey = _config["PayFast:SecuredKey"];
            var returnUrl = _config["PayFast:ReturnUrl"];

            // Note: In a real environment, you must securely generate an Access Token via Server-to-Server call to PayFast's API
            // using the MerchantId and SecuredKey, then return the checkout URL with the token.
            // For now, we return the data needed to construct the payment gateway form on the frontend.

            var payFastData = new
            {
                MerchantId = merchantId,
                SecuredKey = securedKey,
                BasketId = orderId,
                Amount = req.Amount,
                Currency = "PKR",
                ReturnUrl = returnUrl,
                WebhookUrl = $"{Request.Scheme}://{Request.Host}/api/PayFast/webhook",
                CheckoutUrl = $"{Request.Scheme}://{Request.Host}/api/PayFast/checkout/{orderId}"
            };

            return Ok(new
            {
                Message = "Payment initiated successfully",
                TransactionId = transaction.TransactionId,
                OrderId = orderId,
                PayFastData = payFastData
            });
        }

        // GET: api/PayFast/checkout/{orderId}
        [HttpGet("checkout/{orderId}")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> CheckoutForm(string orderId)
        {
            var transaction = await _context.OnlinePaymentTransactions.FirstOrDefaultAsync(t => t.OrderId == orderId);
            if (transaction == null) return NotFound("Transaction not found");

            var merchantId = _config["PayFast:MerchantId"];
            var securedKey = _config["PayFast:SecuredKey"];
            var returnUrl = _config["PayFast:ReturnUrl"];
            var postUrl = _config["PayFast:BaseUrl"];

            // For PayFast, standard integration requires a simple POST form.
            var html = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <title>Redirecting to Secure Payment...</title>
            </head>
            <body onload='document.forms[0].submit()'>
                <div style='text-align:center; margin-top:50px; font-family:sans-serif;'>
                    <h2>Please wait, redirecting to secure payment gateway...</h2>
                </div>
                <form action='{postUrl}' method='POST'>
                    <!-- GoPayFast (Pakistan) Fields -->
                    <input type='hidden' name='merchant_id' value='{merchantId}' />
                    <input type='hidden' name='secured_key' value='{securedKey}' />
                    <input type='hidden' name='basket_id' value='{transaction.OrderId}' />
                    <input type='hidden' name='txampt' value='{transaction.Amount}' />
                    <input type='hidden' name='customer_email_address' value='customer@example.com' />
                    <input type='hidden' name='customer_mobile_no' value='03000000000' />
                    <input type='hidden' name='store_name' value='SmartProMan' />
                    <input type='hidden' name='order_date' value='{transaction.CreatedAt:yyyy-MM-dd HH:mm:ss}' />
                    
                    <!-- PayFast (South Africa) Fields -->
                    <input type='hidden' name='merchant_key' value='{securedKey}' />
                    <input type='hidden' name='return_url' value='{returnUrl}' />
                    <input type='hidden' name='cancel_url' value='{returnUrl}' />
                    <input type='hidden' name='notify_url' value='{Request.Scheme}://{Request.Host}/api/PayFast/webhook' />
                    <input type='hidden' name='m_payment_id' value='{transaction.OrderId}' />
                    <input type='hidden' name='amount' value='{transaction.Amount}' />
                    <input type='hidden' name='item_name' value='Job Payment {transaction.JobId}' />
                </form>
            </body>
            </html>";

            return Content(html, "text/html");
        }

        // POST: api/PayFast/webhook
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook([FromForm] PayFastWebhookPayload payload)
        {
            // PayFast sends response data as form-urlencoded
            if (payload == null || string.IsNullOrEmpty(payload.Basket_id))
                return BadRequest();

            var transaction = await _context.OnlinePaymentTransactions
                .FirstOrDefaultAsync(t => t.OrderId == payload.Basket_id);

            if (transaction == null)
                return NotFound();

            // Validate the transaction state
            if (payload.Err_code == "000" || payload.Err_code == "00") // 00 or 000 typically means Success in banking standards
            {
                transaction.Status = "Paid";
                transaction.CompletedAt = DateTime.Now;
                transaction.GatewayTransactionId = payload.Transaction_id;
                transaction.PaymentMethod = payload.Payment_method;

                var job = await _context.Jobs.FindAsync(transaction.JobId);
                if (job != null)
                {
                    job.Status = "In Progress";
                    job.CompletedAt = null;
                    var tracking = await _context.JobTracking
                        .FirstOrDefaultAsync(t => t.JobID == transaction.JobId);
                    if (tracking != null)
                    {
                        tracking.FCRTime ??= DateTime.Now;
                        tracking.CurrentStep = "Payment Collection";
                        tracking.LastUpdated = DateTime.Now;
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new { Message = "Payment recorded successfully" });
            }
            else
            {
                transaction.Status = "Failed";
                transaction.CompletedAt = DateTime.Now;
                transaction.GatewayTransactionId = payload.Transaction_id;
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Payment failed" });
            }
        }
    }

    public class InitiatePaymentRequest
    {
        public int JobId { get; set; }
        public decimal Amount { get; set; }
    }

    public class PayFastWebhookPayload
    {
        public string Basket_id { get; set; }
        public string Transaction_id { get; set; }
        public string Err_code { get; set; }
        public string Err_msg { get; set; }
        public string Payment_method { get; set; }
    }
}
