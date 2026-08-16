using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using ComicAPI.Application.DTOs.Translation;
using ComicAPI.Domain.Interfaces;
using ComicAPI.Application.Interfaces;
using ComicAPI.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using SharedKernel.Responses;

namespace ComicAPI.Infrastructure.Services;

public sealed class N8nTranslationService : ITranslationService
{
    private static readonly HashSet<string> SupportedLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        "vi", "en", "ja", "ko", "zh", "fr", "de", "es", "th", "id"
    };

    private readonly HttpClient _httpClient;
    private readonly N8nTranslationSettings _settings;
    private readonly ILogger<N8nTranslationService> _logger;

    public N8nTranslationService(
        HttpClient httpClient,
        IOptions<N8nTranslationSettings> options,
        ILogger<N8nTranslationService> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<ApiResponse<TranslateTextResultDto>> TranslateAsync(
        TranslateTextRequestDto request,
        CancellationToken cancellationToken)
    {
        var validationError = Validate(request);
        if (validationError is not null)
        {
            return ApiResponse<TranslateTextResultDto>.ErrorResponse(validationError, 400);
        }

        if (string.IsNullOrWhiteSpace(_settings.WebhookUrl))
        {
            return ApiResponse<TranslateTextResultDto>.ErrorResponse(
                "Dịch ngôn ngữ chưa được cấu hình trên ComicAPI.",
                503);
        }

        var targetLanguage = request.TargetLanguage.Trim().ToLowerInvariant();
        var cleanTexts = request.Texts
            .Select(item => new TranslatableTextDto
            {
                Key = item.Key.Trim(),
                Value = item.Value.Trim()
            })
            .ToList();

        using var message = new HttpRequestMessage(HttpMethod.Post, _settings.WebhookUrl)
        {
            Content = JsonContent.Create(new
            {
                targetLanguage,
                texts = cleanTexts.Select(item => new { item.Key, item.Value })
            })
        };

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_settings.TimeoutSeconds, 5, 120)));

        try
        {
            using var response = await _httpClient.SendAsync(message, timeoutSource.Token);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(timeoutSource.Token);
                _logger.LogWarning(
                    "n8n translation returned {StatusCode}: {ResponseBody}",
                    (int)response.StatusCode,
                    responseBody);
                var clientMessage = response.StatusCode switch
                {
                    HttpStatusCode.NotFound =>
                        "Workflow dịch trên n8n chưa được Publish hoặc URL webhook chưa đúng.",
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                        "n8n từ chối yêu cầu webhook. Hãy kiểm tra workflow đã được Publish và đang chạy nội bộ.",
                    _ => "n8n không thể xử lý yêu cầu dịch. Hãy kiểm tra Executions trên n8n."
                };
                return ApiResponse<TranslateTextResultDto>.ErrorResponse(clientMessage, 502);
            }

            var result = await response.Content.ReadFromJsonAsync<TranslateTextResultDto>(
                cancellationToken: timeoutSource.Token);
            if (result is null
                || !string.Equals(result.TargetLanguage, targetLanguage, StringComparison.OrdinalIgnoreCase)
                || result.Items.Count != cleanTexts.Count
                || result.Items.Select(item => item.Key).Distinct().Count() != cleanTexts.Count)
            {
                return ApiResponse<TranslateTextResultDto>.ErrorResponse(
                    "n8n trả về dữ liệu dịch không hợp lệ.",
                    502);
            }

            var expectedKeys = cleanTexts.Select(item => item.Key).ToHashSet(StringComparer.Ordinal);
            if (result.Items.Any(item => !expectedKeys.Contains(item.Key) || string.IsNullOrWhiteSpace(item.Value)))
            {
                return ApiResponse<TranslateTextResultDto>.ErrorResponse(
                    "n8n trả về dữ liệu dịch không khớp.",
                    502);
            }

            return new ApiResponse<TranslateTextResultDto>(result, "Dịch phần chữ thành công.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ApiResponse<TranslateTextResultDto>.ErrorResponse(
                "n8n phản hồi quá thời gian cho phép.",
                504);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Could not call the n8n translation webhook.");
            return ApiResponse<TranslateTextResultDto>.ErrorResponse(
                "Không thể kết nối tới n8n.",
                502);
        }
    }

    private static string? Validate(TranslateTextRequestDto request)
    {
        if (request.Texts is null)
        {
            return "Danh sách nội dung cần dịch là bắt buộc.";
        }

        if (request.AdditionalFields?.Count > 0
            || request.Texts.Any(item => item is null || item.AdditionalFields?.Count > 0))
        {
            return "Chức năng này chỉ nhận dữ liệu chữ; không nhận ảnh hoặc trường dữ liệu khác.";
        }

        if (!SupportedLanguages.Contains(request.TargetLanguage?.Trim() ?? string.Empty))
        {
            return "Ngôn ngữ đích không được hỗ trợ.";
        }

        if (request.Texts.Count is < 1 or > 250)
        {
            return "Mỗi yêu cầu phải có từ 1 đến 250 đoạn chữ.";
        }

        var keys = new HashSet<string>(StringComparer.Ordinal);
        var totalCharacters = 0;
        foreach (var item in request.Texts)
        {
            var key = item.Key?.Trim() ?? string.Empty;
            var value = item.Value?.Trim() ?? string.Empty;
            if (key.Length is < 1 or > 200 || !keys.Add(key))
            {
                return "Mỗi đoạn chữ phải có key duy nhất, tối đa 200 ký tự.";
            }

            if (value.Length is < 1 or > 5000)
            {
                return "Mỗi đoạn dịch phải có từ 1 đến 5000 ký tự.";
            }

            totalCharacters += value.Length;
        }

        return totalCharacters > 30000
            ? "Tổng nội dung dịch không được vượt quá 30000 ký tự."
            : null;
    }
}
