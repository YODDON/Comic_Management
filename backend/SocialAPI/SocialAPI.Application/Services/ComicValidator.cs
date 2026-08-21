using System.Net;
using Microsoft.Extensions.Logging;
using SocialAPI.Interfaces;

namespace SocialAPI.Services
{
    public class ComicValidator : IComicValidator
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ComicValidator> _logger;

        public ComicValidator(HttpClient httpClient, ILogger<ComicValidator> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<bool> ExistsAsync(Guid comicId)
        {
            try
            {
                using var response = await _httpClient.GetAsync($"/comics/{comicId}");
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                if (response.StatusCode != HttpStatusCode.NotFound)
                {
                    _logger.LogWarning(
                        "Comic validation failed for {ComicId} with upstream status {StatusCode}",
                        comicId,
                        (int)response.StatusCode);
                }

                return false;
            }
            catch (HttpRequestException exception)
            {
                _logger.LogWarning(exception, "Comic validation request failed for {ComicId}", comicId);
                return false;
            }
            catch (TaskCanceledException exception)
            {
                _logger.LogWarning(exception, "Comic validation request timed out for {ComicId}", comicId);
                return false;
            }
        }
    }
}
