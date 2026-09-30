using Microsoft.EntityFrameworkCore;
using SmartProManWebAPI.Models;

namespace SmartProManWebAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Technician> Technicians { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<DispatchBid> DispatchBids { get; set; }
        public DbSet<JobTracking> JobTracking { get; set; }
        public DbSet<TechnicianLocation> TechnicianLocations { get; set; }
        public DbSet<UnitRegistration> UnitRegistrations { get; set; }
        public DbSet<UnitRegistrationMedia> UnitRegistrationMedia { get; set; }
        public DbSet<DailyWorkReport> DailyWorkReports { get; set; }
        public DbSet<MasterCard> MasterCards { get; set; }
        public DbSet<ProformaInvoice> ProformaInvoices { get; set; }
        public DbSet<ProformaInvoiceItem> ProformaInvoiceItems { get; set; }
        public DbSet<JobMessage> JobMessages { get; set; }
        public DbSet<JobStuckHistory> JobStuckHistories { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AdminNotification> AdminNotifications { get; set; }
        public DbSet<Request> Requests { get; set; }
        public DbSet<OnlinePaymentTransaction> OnlinePaymentTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Set OnDelete Cascade for relationships
            var cascadeFKs = modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetForeignKeys())
                .Where(fk => !fk.IsOwnership && fk.DeleteBehavior != DeleteBehavior.Cascade);

            foreach (var fk in cascadeFKs)
            {
                fk.DeleteBehavior = DeleteBehavior.Cascade;
            }
        }
    }
}
