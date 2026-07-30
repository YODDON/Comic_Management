using System;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using ComicAPI.Interfaces;
using ComicAPI.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ComicAPI.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IOptions<CloudinarySettings> config)
        {
            var acc = new Account(
                config.Value.CloudName,
                config.Value.ApiKey,
                config.Value.ApiSecret
            );

            _cloudinary = new Cloudinary(acc);
        }

        public async Task<string> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0) return string.Empty;

            var uploadResult = new ImageUploadResult();

            using (var stream = file.OpenReadStream())
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Transformation = new Transformation().Width(600).Height(800).Crop("fill").Gravity("auto")
                };

                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }

            return uploadResult.SecureUrl?.ToString() ?? string.Empty;
        }
    }
}
