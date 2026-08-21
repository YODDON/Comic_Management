using Microsoft.EntityFrameworkCore;
using WalletAPI.Entities;
using MassTransit;

namespace WalletAPI.Data;

public class WalletDbContext : DbContext
{
    public WalletDbContext(DbContextOptions<WalletDbContext> options) : base(options)
    {
    }

    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<CurrencyEntry> CurrencyEntries => Set<CurrencyEntry>();
    public DbSet<Withdraw> Withdraws => Set<Withdraw>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.Property(x => x.Balance).HasPrecision(18, 2);
        });

        modelBuilder.Entity<CurrencyEntry>(entity =>
        {
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(x => x.WalletId);
            entity.HasIndex(x => x.ReferenceId).IsUnique().HasFilter("[ReferenceId] IS NOT NULL");
            entity.HasOne(x => x.Wallet)
                .WithMany(x => x.CurrencyEntries)
                .HasForeignKey(x => x.WalletId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Withdraw>(entity =>
        {
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.BankAccount).HasMaxLength(255);
            entity.Property(x => x.BankName).HasMaxLength(255);
            entity.Property(x => x.AccountName).HasMaxLength(255);
            entity.Property(x => x.Note).HasMaxLength(1000);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(x => x.WalletId);
            entity.HasIndex(x => x.WalletId)
                .HasDatabaseName("IX_Withdraws_WalletId_Pending")
                .IsUnique()
                .HasFilter("[Status] = 'Pending'");
            entity.HasOne(x => x.Wallet)
                .WithMany(x => x.Withdraws)
                .HasForeignKey(x => x.WalletId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
