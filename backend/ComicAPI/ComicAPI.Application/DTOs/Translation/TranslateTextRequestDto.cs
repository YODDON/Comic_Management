using System.Text.Json;
using System.Text.Json.Serialization;

namespace ComicAPI.Application.DTOs.Translation;

public sealed class TranslateTextRequestDto
{
    public string TargetLanguage { get; set; } = string.Empty;
    public List<TranslatableTextDto> Texts { get; set; } = new();

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalFields { get; set; }
}

public sealed class TranslatableTextDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalFields { get; set; }
}

public sealed class TranslateTextResultDto
{
    public string TargetLanguage { get; set; } = string.Empty;
    public List<TranslatedTextDto> Items { get; set; } = new();
}

public sealed class TranslatedTextDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
