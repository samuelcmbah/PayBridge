using Microsoft.EntityFrameworkCore;
using PayBridge.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayBridge.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            
        }
        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Make Reference unique and indexed for fast lookups during webhooks
                entity.HasIndex(e => e.Reference).IsUnique();

                // Store Enums as strings in the DB for better readability
                entity.Property(e => e.Status).HasConversion<string>();
                entity.Property(e => e.Provider).HasConversion<string>();
                entity.Property(e => e.Purpose).HasConversion<string>();

                // Ensure decimal precision for money
                entity.Property(e => e.Amount).HasPrecision(18, 2);
            });
        }
    }
}
