using BannerAPI.Data;
using BannerAPI.Entities;
using BannerAPI.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BannerAPI.Repositories;

public class BannerRepository : IBannerRepository
{
    private readonly BannerDbContext _context;

    public BannerRepository(BannerDbContext context)
    {
        _context = context;
    }

    public Task<List<Banner>> GetActiveAsync() => _context.Banners
        .AsNoTracking()
        .Where(x => x.IsActive)
        .OrderBy(x => x.DisplayOrder)
        .ThenByDescending(x => x.CreatedAt)
        .ToListAsync();

    public Task<List<Banner>> GetAllAsync() => _context.Banners
        .AsNoTracking()
        .OrderBy(x => x.DisplayOrder)
        .ThenByDescending(x => x.CreatedAt)
        .ToListAsync();

    public Task<Banner?> GetByIdAsync(Guid id) => _context.Banners.SingleOrDefaultAsync(x => x.Id == id);

    public Task<bool> TitleExistsAsync(string title, Guid? excludingId = null)
    {
        var normalizedTitle = title.Trim().ToLower();
        return _context.Banners.AnyAsync(x =>
            x.Title != null
            && x.Title.Trim().ToLower() == normalizedTitle
            && (!excludingId.HasValue || x.Id != excludingId.Value));
    }

    public Task<bool> DisplayOrderExistsAsync(int displayOrder, Guid? excludingId = null) =>
        _context.Banners.AnyAsync(x =>
            x.DisplayOrder == displayOrder
            && (!excludingId.HasValue || x.Id != excludingId.Value));

    public async Task AddAsync(Banner banner)
    {
        _context.Banners.Add(banner);
        await _context.SaveChangesAsync();
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();

    public void Remove(Banner banner) => _context.Banners.Remove(banner);
}
