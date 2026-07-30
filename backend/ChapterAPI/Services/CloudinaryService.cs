using System;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using ChapterAPI.Interfaces;
using ChapterAPI.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ChapterAPI.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IOptions<CloudinarySettings> config)
        {
            var cloudName = Environment.GetEnvironmentVariable("Cloudinary__CloudName") ?? config.Value.CloudName;
            var apiKey = Environment.GetEnvironmentVariable("Cloudinary__ApiKey") ?? config.Value.ApiKey;
            var apiSecret = Environment.GetEnvironmentVariable("Cloudinary__ApiSecret") ?? config.Value.ApiSecret;

            var acc = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(acc);
        }

        public async Task<string> UploadImageAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0) return string.Empty;

            var uploadResult = new ImageUploadResult();

            using (var stream = file.OpenReadStream())
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = folder
                };

                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }

            return uploadResult.SecureUrl?.ToString() ?? string.Empty;
        }

        public async Task<bool> DeleteImageAsync(string publicId)
        {
            var deleteParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deleteParams);
            return result.Result == "ok";
        }
    }
}
