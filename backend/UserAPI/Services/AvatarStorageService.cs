using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using UserAPI.Interfaces;

namespace UserAPI.Services;

public class AvatarStorageService : IAvatarStorageService
{
    private readonly Cloudinary _cloudinary;

    public AvatarStorageService(IConfiguration configuration)
    {
        var account = new Account(
            configuration["Cloudinary:CloudName"],
            configuration["Cloudinary:ApiKey"],
            configuration["Cloudinary:ApiSecret"]);
        _cloudinary = new Cloudinary(account) { Api = { Secure = true } };
    }

    public async Task<string> UploadAsync(IFormFile file, int userId)
    {
        await using var stream = file.OpenReadStream();
        var upload = await _cloudinary.UploadAsync(new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = "comico/avatars",
            PublicId = $"user-{userId}",
            Overwrite = true,
            Invalidate = true,
            Transformation = new Transformation()
                .Width(512).Height(512).Crop("fill").Gravity("face")
                .Quality("auto").FetchFormat("auto")
        });

        if (upload.Error is not null)
        {
            throw new InvalidOperationException(upload.Error.Message);
        }

        return upload.SecureUrl?.ToString()
            ?? throw new InvalidOperationException("Cloudinary did not return an avatar URL.");
    }
}
