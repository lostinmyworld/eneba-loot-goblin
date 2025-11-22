using System.Text.Json;
using System.Text.Json.Serialization;

namespace EnebaLootGoblin;

internal static class DiscordBuilder
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    internal static string BuildDiscordPayload(List<Offer> offers, decimal minPrice)
    {
        var lines = new List<string>();

        foreach (var offer in offers)
        {
            var line = $"- **{offer.Title}**: {offer.Price:F2} €";

            if (!string.IsNullOrWhiteSpace(offer.Url))
            {
                line += $"-- [Πάτα ΕΔΩ]({offer.Url})";
            }

            lines.Add(line);
        }

        var description = string.Join("\n", lines);
        var imageUrl = offers.LastOrDefault(offer => !string.IsNullOrWhiteSpace(offer.ImageUrl))?.ImageUrl;

        var embed = new Dictionary<string, object?>
        {
            ["title"] = $"Χαμηλές τιμές στην Eneba - κάτω των {minPrice} €",
            ["description"] = description,
        };

        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            embed["image"] = new { url = imageUrl };
        }

        var payload = new
        {
            embeds = new[] { embed }
        };

        return JsonSerializer.Serialize(payload, _jsonOptions);
    }
}
