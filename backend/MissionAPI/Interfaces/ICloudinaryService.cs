using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MissionAPI.Interfaces
{
    public interface ICloudinaryService
    {
        Task<CloudinaryDotNet.Actions.ImageUploadResult> UploadImageAsync(IFormFile file);
    }
}
