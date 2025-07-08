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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Optional: Configure precision for decimal types if needed
            // modelBuilder.Entity<InsuranceContract>()
            //     .Property(i => i.InsRate)
            //     .HasColumnType("decimal(5, 2)");

            base.OnModelCreating(modelBuilder);
        }
    }
}
