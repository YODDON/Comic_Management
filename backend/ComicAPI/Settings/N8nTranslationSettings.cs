namespace ComicAPI.Settings;

public sealed class N8nTranslationSettings
{
    public const string SectionName = "N8nTranslation";

    public string WebhookUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 65;
}
