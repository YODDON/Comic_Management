using BannerAPI.Entities;

namespace BannerAPI.Interfaces;

public interface IBannerRepository
{
    Task<List<Banner>> GetActiveAsync();
    Task<List<Banner>> GetAllAsync();
    Task<Banner?> GetByIdAsync(Guid id);
    Task<bool> TitleExistsAsync(string title, Guid? excludingId = null);
    Task<bool> DisplayOrderExistsAsync(int displayOrder, Guid? excludingId = null);
    Task AddAsync(Banner banner);
    Task SaveChangesAsync();
    void Remove(Banner banner);
}
