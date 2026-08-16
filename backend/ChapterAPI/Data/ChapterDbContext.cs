using Microsoft.EntityFrameworkCore;
using ChapterAPI.Entities;
using MassTransit;

namespace ChapterAPI.Data
{
    public class ChapterDbContext : DbContext
    {
        public ChapterDbContext(DbContextOptions<ChapterDbContext> options) : base(options)
        {
        }

        public DbSet<Chapter> Chapters { get; set; }
        public DbSet<ChapterPage> ChapterPages { get; set; }
        public DbSet<UserPurchase> UserPurchases { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Chapter>()
                .HasMany(c => c.ChapterPages)
                .WithOne(cp => cp.Chapter)
                .HasForeignKey(cp => cp.ChapterId);

            modelBuilder.Entity<Chapter>()
                .Property(c => c.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<UserPurchase>(entity =>
            {
                entity.HasIndex(up => new { up.UserId, up.ChapterId })
                    .IsUnique();
            });

            modelBuilder.AddInboxStateEntity();
            modelBuilder.AddOutboxMessageEntity();
            modelBuilder.AddOutboxStateEntity();
        }
    }
}
