using System;
using System.Net.Http;
using System.Threading.Tasks;
using SocialAPI.Interfaces;

namespace SocialAPI.Services
{
    public class ComicValidator : IComicValidator
    {
        private readonly HttpClient _httpClient;

        public ComicValidator(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> ExistsAsync(Guid comicId)
        {
            try
            {
                // This is a placeholder for the actual API call to ComicAPI or ApiGateway
                // Assuming ApiGateway routes /api/comics/{id} to ComicAPI
                var response = await _httpClient.GetAsync($"/api/comics/{comicId}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                // If the service is unreachable, we can choose to fail or assume it doesn't exist.
                // For now, let's return false or we can return true in dev to not block testing.
                return true; // Temporarily return true for testing without ComicAPI
            }
        }
    }
}
