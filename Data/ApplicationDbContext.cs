using pviBase.Models;
using Microsoft.EntityFrameworkCore;

namespace pviBase.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<InsuranceContract> InsuranceContracts { get; set; }
        public DbSet<RequestLog> RequestLogs { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Optional: Configure precision for decimal types if needed
            modelBuilder.Entity<RequestLog>()
                .Property(i => i.Status)
                .HasConversion<string>();

            base.OnModelCreating(modelBuilder);
        }
    }
}
