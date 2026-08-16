using Microsoft.AspNetCore.Http;
namespace UserAPI.Application.Interfaces;

public interface IAvatarStorageService
{
    Task<string> UploadAsync(IFormFile file, int userId);
}
