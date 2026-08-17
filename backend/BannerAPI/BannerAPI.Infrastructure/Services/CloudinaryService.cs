using BannerAPI.Interfaces;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace BannerAPI.Services;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IConfiguration configuration)
    {
        var account = new Account(
            Environment.GetEnvironmentVariable("Cloudinary__CloudName")
                ?? Environment.GetEnvironmentVariable("CloudinarySettings__CloudName")
                ?? configuration["Cloudinary:CloudName"],
            Environment.GetEnvironmentVariable("Cloudinary__ApiKey")
                ?? Environment.GetEnvironmentVariable("CloudinarySettings__ApiKey")
                ?? configuration["Cloudinary:ApiKey"],
            Environment.GetEnvironmentVariable("Cloudinary__ApiSecret")
                ?? Environment.GetEnvironmentVariable("CloudinarySettings__ApiSecret")
                ?? configuration["Cloudinary:ApiSecret"]);
        _cloudinary = new Cloudinary(account);
    }

    public async Task<(string Url, string PublicId)> UploadImageAsync(IFormFile file)
    {
        if (file.Length == 0) throw new ArgumentException("File ảnh không được để trống.");
        if (file.Length > 10 * 1024 * 1024) throw new ArgumentException("Ảnh banner không được vượt quá 10 MB.");
        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Chỉ chấp nhận file ảnh.");

        await using var stream = file.OpenReadStream();
        var result = await _cloudinary.UploadAsync(new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = "comico/banners",
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = false
        });
        if (result.Error is not null) throw new InvalidOperationException(result.Error.Message);
        return (result.SecureUrl.ToString(), result.PublicId);
    }

    public async Task<bool> DeleteImageAsync(string publicId)
    {
        var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId)
        {
            ResourceType = ResourceType.Image,
            Invalidate = true
        });
        return result.Result is "ok" or "not found";
    }
}
