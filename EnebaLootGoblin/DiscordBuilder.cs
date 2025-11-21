using System.Text.Json;
using System.Text.Json.Serialization;

namespace EnebaLootGoblin;

internal static class DiscordBuilder
{
    internal static string BuildDiscordPayload(List<Offer> offers)
    {
        var lines = new List<string>();

        foreach (var offer in offers)
        {
            var line = $"- **{offer.Title}**: {offer.Discount}% off " +
                $"(~~€{offer.OldPrice:F2}~~ → €{offer.Price:F2})";

            if (!string.IsNullOrWhiteSpace(offer.Url))
            {
                line += $" [Link]({offer.Url})";
            }

            lines.Add(line);
        }

        var description = "Οι καλύτερες προσφορές Eneba για αυτή την εβδομάδα:\n\n"
            + string.Join("\n", lines);
        var imageUrl = offers.FirstOrDefault(offer => !string.IsNullOrWhiteSpace(offer.ImageUrl))?.ImageUrl;

        var embed = new Dictionary<string, object?>
        {
            ["title"] = "Eneba Loot Goblin - Weekly Deals",
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

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        });

        return json;
    }
}
