using System.Text.Json.Serialization;

namespace EnebaLootGoblin.Models;

public class DiscordRequest
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("embeds")]
    public Dictionary<string, object?>[] Embeds { get; set; } = [];
}
