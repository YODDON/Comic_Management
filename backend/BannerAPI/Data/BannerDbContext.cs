using BannerAPI.Entities;
using Microsoft.EntityFrameworkCore;

namespace BannerAPI.Data
{
    public class BannerDbContext : DbContext
    {
        public BannerDbContext(DbContextOptions<BannerDbContext> options) : base(options)
        {
        }

        public DbSet<Banner> Banners { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Banner>(entity =>
            {
                entity.Property(x => x.Title).HasMaxLength(200).IsRequired(false);
                entity.Property(x => x.ImageUrl).HasMaxLength(2048);
                entity.Property(x => x.ImagePublicId).HasMaxLength(500);
                entity.Property(x => x.TargetUrl).HasMaxLength(2048).IsRequired(false);
                entity.HasIndex(x => new { x.IsActive, x.DisplayOrder });
            });
        }
    }
}
