namespace EnebaLootGoblin.Models;

public record EnvironmentVariables(
    string? EnebaFeedUrl,
    string? DiscordWebHook,
    decimal MaxPrice,
    int MaxOffers);
