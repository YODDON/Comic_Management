using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ChapterAPI.Interfaces
{
    public interface ICloudinaryService
    {
        Task<string> UploadImageAsync(IFormFile file, string folder);
        Task<bool> DeleteImageAsync(string publicId);
    }
}
