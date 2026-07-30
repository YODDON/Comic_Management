using Microsoft.EntityFrameworkCore;
using ComicAPI.Entities;

namespace ComicAPI.Data
{
    public class ComicDbContext : DbContext
    {
        public ComicDbContext(DbContextOptions<ComicDbContext> options) : base(options)
        {
        }

        public DbSet<Comic> Comics { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<ComicCategory> ComicCategories { get; set; }
        public DbSet<Outstanding> Outstandings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ComicCategory>()
                .HasOne(cc => cc.Comic)
                .WithMany(c => c.ComicCategories)
                .HasForeignKey(cc => cc.ComicId);

            modelBuilder.Entity<ComicCategory>()
                .HasOne(cc => cc.Category)
                .WithMany(c => c.ComicCategories)
                .HasForeignKey(cc => cc.CategoryId);

            modelBuilder.Entity<Outstanding>()
                .ToTable("Outstanding")
                .HasOne(o => o.Comic)
                .WithMany(c => c.Outstandings)
                .HasForeignKey(o => o.ComicId);

            modelBuilder.Entity<Comic>()
                .Property(c => c.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Comic>()
                .Property(c => c.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Category>().HasData(
                new Category { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Action", Slug = "action", Tag = "Action-packed comics", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Category { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Romance", Slug = "romance", Tag = "Romantic stories", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Category { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Comedy", Slug = "comedy", Tag = "Funny and hilarious", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Category { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = "Fantasy", Slug = "fantasy", Tag = "Magical worlds", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Category { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Name = "Horror", Slug = "horror", Tag = "Scary and thrilling", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Category { Id = Guid.Parse("66666666-6666-6666-6666-666666666666"), Name = "Sci-Fi", Slug = "sci-fi", Tag = "Science fiction", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Category { Id = Guid.Parse("77777777-7777-7777-7777-777777777777"), Name = "Slice of Life", Slug = "slice-of-life", Tag = "Everyday life", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Category { Id = Guid.Parse("88888888-8888-8888-8888-888888888888"), Name = "Drama", Slug = "drama", Tag = "Emotional and dramatic", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            );
        }
    }
}
