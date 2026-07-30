namespace UserAPI.Interfaces;

public interface IAvatarStorageService
{
    Task<string> UploadAsync(IFormFile file, int userId);
}
