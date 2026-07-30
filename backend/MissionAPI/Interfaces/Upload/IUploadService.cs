using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using MissionAPI.DTOs;
using SharedKernel.Responses;

namespace MissionAPI.Interfaces
{
    public interface IUploadService
    {
        Task<ApiResponse<UploadResponseDto>> UploadImageAsync(int userId, IFormFile file);
    }
}
