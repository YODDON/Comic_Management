using Microsoft.EntityFrameworkCore;
using PaymentAPI.Domain.Entities;
using PaymentAPI.Entities;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;

namespace PaymentAPI.Data
{
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
        {
        }

        public DbSet<UserPurchase> UserPurchases { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<PurchaseState> PurchaseStates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.AddInboxStateEntity();
            modelBuilder.AddOutboxMessageEntity();
            modelBuilder.AddOutboxStateEntity();
            
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.Property(x => x.Amount).HasPrecision(18, 2);
                entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
                entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
                entity.Property(x => x.Note).HasMaxLength(1000);
                entity.HasIndex(x => x.TransactionCode).IsUnique().HasFilter("[TransactionCode] <> ''");
                entity.HasIndex(x => x.ExternalTransactionId).IsUnique().HasFilter("[ExternalTransactionId] IS NOT NULL");
            });
            modelBuilder.Entity<UserPurchase>(entity =>
            {
                entity.Property(x => x.Price).HasPrecision(18, 2);
                entity.HasIndex(x => new { x.UserId, x.ChapterId }).IsUnique();
            });
            modelBuilder.Entity<PurchaseState>(entity =>
            {
                entity.HasKey(x => x.CorrelationId);
                entity.Property(x => x.CurrentState).HasMaxLength(64);
                entity.Property(x => x.Price).HasPrecision(18, 2);
            });
        }
    }
}
