using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ComicAPI.Interfaces
{
    public interface ICloudinaryService
    {
        Task<string> UploadImageAsync(IFormFile file);
    }
}
