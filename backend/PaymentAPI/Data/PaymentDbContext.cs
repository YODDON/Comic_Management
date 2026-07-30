using Microsoft.EntityFrameworkCore;
using PaymentAPI.Entities;

namespace PaymentAPI.Data
{
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
        {
        }

        public DbSet<UserPurchase> UserPurchases { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.Property(x => x.Amount).HasPrecision(18, 2);
                entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
                entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
                entity.Property(x => x.Note).HasMaxLength(1000);
                entity.HasIndex(x => x.TransactionCode).IsUnique().HasFilter("[TransactionCode] <> ''");
            });
            modelBuilder.Entity<UserPurchase>(entity =>
            {
                entity.Property(x => x.Price).HasPrecision(18, 2);
                entity.HasIndex(x => new { x.UserId, x.ChapterId }).IsUnique();
            });
        }
    }
}
