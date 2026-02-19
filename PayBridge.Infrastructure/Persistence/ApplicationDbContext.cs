using Microsoft.EntityFrameworkCore;
using PayBridge.Domain.Entities;
using PayBridge.Domain.ValueObjects;
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

                //entity.Property<byte[]>("RowVersion") //Race Condition & Optimistic Concurrency
                //   .IsRowVersion()           
                //   .HasColumnName("RowVersion")
                //   .IsRequired();

                entity.Property(e => e.Reference)
                    .HasConversion(
                        v => v.Value,                           // To database
                        v => PaymentReference.Create(v)         // From database
                    )
                    .HasColumnName("Reference")
                    .HasMaxLength(50)
                    .IsRequired();

                // Make Reference unique and indexed for fast lookups during webhooks
                entity.HasIndex(e => e.Reference).IsUnique();

                entity.OwnsOne(e => e.Amount, money =>
                {
                    money.Property(m => m.Amount)
                        .HasColumnName("Amount")
                        .HasPrecision(18, 2)
                        .IsRequired();

                    money.Property(m => m.Currency)
                        .HasColumnName("Currency")
                        .HasMaxLength(3)
                        .IsRequired();
                });

                entity.Property(e => e.ExternalUserId)
                    .HasConversion(
                        v => v.Value,
                        v => Email.Create(v)
                    )
                    .HasColumnName("ExternalUserId")
                    .HasMaxLength(254)
                    .IsRequired();

                entity.Property(e => e.RedirectUrl)
                   .HasConversion(
                       v => v.Value,
                       v => Url.Create(v)
                   )
                   .HasColumnName("RedirectUrl")
                   .HasMaxLength(500)
                   .IsRequired();

                entity.Property(e => e.NotificationUrl)
                   .HasConversion(
                       v => v.Value,
                       v => Url.Create(v)
                   )
                   .HasColumnName("NotificationUrl")
                   .HasMaxLength(500)
                   .IsRequired();


                // Store Enums as strings in the DB for better readability
                entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(e => e.Provider)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.Purpose)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();


                //other properties
                entity.Property(e => e.AppName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(e => e.ExternalReference)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .IsRequired();

                entity.Property(e => e.VerifiedAt)
                    .IsRequired(false);
            });
        }
    }
}
