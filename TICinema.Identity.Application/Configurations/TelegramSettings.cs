namespace TICinema.Identity.Infrastructure.Configurations;

public class TelegramSettings
{
    public const string SectionName = "TelegramSettings";
    public long TelegramBotId { get; set; }
    public string TelegramBotToken { get; set; } = string.Empty;
    public string TelegramBotUsername { get; set; } = string.Empty;
    public string TelegramRedirectOrigin { get; set; } = string.Empty;
}