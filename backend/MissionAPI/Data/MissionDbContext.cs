using Microsoft.EntityFrameworkCore;
using MissionAPI.Entities;

namespace MissionAPI.Data
{
    public class MissionDbContext : DbContext
    {
        public MissionDbContext(DbContextOptions<MissionDbContext> options) : base(options)
        {
        }

        public DbSet<Upload> Uploads { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Mission> Missions { get; set; }
        public DbSet<UserMission> UserMissions { get; set; }
        public DbSet<MissionActivity> MissionActivities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Mission>()
                .HasMany(m => m.UserMissions)
                .WithOne(um => um.Mission)
                .HasForeignKey(um => um.MissionId);
            modelBuilder.Entity<UserMission>().HasIndex(x => new { x.UserId, x.MissionId }).IsUnique();
            modelBuilder.Entity<MissionActivity>(entity =>
            {
                entity.HasIndex(x => new { x.UserId, x.MissionId, x.ActivityId }).IsUnique();
                entity.HasOne(x => x.Mission)
                    .WithMany()
                    .HasForeignKey(x => x.MissionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<Mission>().Property(x => x.RewardCoin).HasPrecision(18, 2);
            modelBuilder.Entity<Mission>().Property(x => x.Type).HasConversion<string>();

            modelBuilder.Entity<Mission>().HasData(
                new Mission
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Title = "Đọc Truyện Chăm Chỉ",
                    Description = "Đọc ít nhất 5 chương truyện bất kỳ.",
                    RewardCoin = 50,
                    Type = SharedKernel.Enums.MissionType.ReadChapter,
                    TargetCount = 5,
                    IsActive = true,
                    StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Mission
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Title = "Đại Gia Nạp Lần Đầu",
                    Description = "Nạp ít nhất 50.000 VNĐ vào ví.",
                    RewardCoin = 200,
                    Type = SharedKernel.Enums.MissionType.PurchaseChapter,
                    TargetCount = 1,
                    IsActive = true,
                    StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Mission
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Title = "Nhà Phê Bình Bàn Phím",
                    Description = "Bình luận ít nhất 3 lần vào các chương truyện.",
                    RewardCoin = 30,
                    Type = SharedKernel.Enums.MissionType.LeaveComment,
                    TargetCount = 3,
                    IsActive = true,
                    StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}
