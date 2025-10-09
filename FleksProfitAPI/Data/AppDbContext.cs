using FleksProfitAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FleksProfitAPI.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<FcrRecord> FcrRecords { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // HourUTC skal være unik
            modelBuilder.Entity<FcrRecord>()
                .HasIndex(f => f.HourUTC)
                .IsUnique();
        }
    }
}
