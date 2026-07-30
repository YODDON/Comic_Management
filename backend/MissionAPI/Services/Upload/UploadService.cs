using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using MissionAPI.DTOs;
using MissionAPI.Entities;
using MissionAPI.Interfaces;
using SharedKernel.Responses;

namespace MissionAPI.Services
{
    public class UploadService : IUploadService
    {
        private readonly IUploadRepository _uploadRepository;
        private readonly ICloudinaryService _cloudinaryService;

        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long _maxFileSize = 5 * 1024 * 1024;

        public UploadService(IUploadRepository uploadRepository, ICloudinaryService cloudinaryService)
        {
            _uploadRepository = uploadRepository;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<ApiResponse<UploadResponseDto>> UploadImageAsync(int userId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return new ApiResponse<UploadResponseDto>(null, "No file uploaded", 422);
            }

            if (file.Length > _maxFileSize)
            {
                return new ApiResponse<UploadResponseDto>(null, "File size exceeds 5MB limit", 422);
            }

            var extension = System.IO.Path.GetExtension(file.FileName).ToLower();
            if (!_allowedExtensions.Contains(extension))
            {
                return new ApiResponse<UploadResponseDto>(null, "Invalid file format. Allowed: jpg, jpeg, png, webp", 422);
            }

            var uploadResult = await _cloudinaryService.UploadImageAsync(file);
            if (uploadResult.Error != null)
            {
                return new ApiResponse<UploadResponseDto>(null, $"Cloudinary upload failed: {uploadResult.Error.Message}", 500);
            }

            var url = uploadResult.SecureUrl.AbsoluteUri;
            var publicId = uploadResult.PublicId;

            var uploadRecord = new Upload
            {
                UserId = userId,
                FileType = extension,
                Url = url,
                FileSize = file.Length,
                PublicId = publicId
            };

            await _uploadRepository.CreateUploadRecordAsync(uploadRecord);

            var dto = new UploadResponseDto
            {
                Url = url,
                PublicId = publicId
            };

            return new ApiResponse<UploadResponseDto>(dto, "File uploaded successfully");
        }
    }
}
