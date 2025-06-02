namespace Logistiq.Infrastructure.Settings;

public class StripeSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public Dictionary<string, string> PriceIds { get; set; } = new();
}