using Microsoft.AspNetCore.Http;

namespace BannerAPI.Interfaces;

public interface ICloudinaryService
{
    Task<(string Url, string PublicId)> UploadImageAsync(IFormFile file);
    Task<bool> DeleteImageAsync(string publicId);
}
