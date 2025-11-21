using System.Net.Mime;
using System.Text;
using EnebaLootGoblin;
using File = System.IO.File;

LoadLocalEnv(".env.local");

var feedUrl = Environment.GetEnvironmentVariable("ENEBA_FEED_URL")
    ?? throw new InvalidOperationException("'ENEBA_FEED_URL' is not set.");

var webhookUrl = Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_URL")
    ?? throw new InvalidOperationException("'DISCORD_WEBHOOK_URL' is not set.");

var minDiscount = int.TryParse(Environment.GetEnvironmentVariable("MIN_DISCOUNT"), out var discountValue)
    ? discountValue
    : 40;

var maxOffers = int.TryParse(Environment.GetEnvironmentVariable("MAX_OFFERS"), out var maxOffersValue)
    ? maxOffersValue
    : 3;
Console.WriteLine("ENEBA_FEED_URL = " + Environment.GetEnvironmentVariable("ENEBA_FEED_URL"));
Console.WriteLine("DISCORD_WEBHOOK_URL = " + Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_URL"));
Console.WriteLine("MIN_DISCOUNT = " + Environment.GetEnvironmentVariable("MIN_DISCOUNT"));
Console.WriteLine("MAX_OFFERS = " + Environment.GetEnvironmentVariable("MAX_OFFERS"));

try
{
    using var httpClient = new HttpClient();

    Console.WriteLine("Downloading feed...");

    var csv = feedUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
        ? await httpClient.GetStringAsync(feedUrl)
        : await File.ReadAllTextAsync(feedUrl);

    var offers = Parser.ParseOffers(csv, minDiscount);

    if (offers.Count == 0)
    {
        Console.WriteLine("No offers found with the specified discount.");
        return;
    }

    var best = offers.OrderByDescending(offer => offer.Discount)
        .Take(maxOffers)
        .ToList();

    var payloadJson = DiscordBuilder.BuildDiscordPayload(offers);

    Console.WriteLine("Posting to Discord...");
    var content = new StringContent(payloadJson, Encoding.UTF8, MediaTypeNames.Application.Json);
    var response = await httpClient.PostAsync(webhookUrl, content);

    Console.WriteLine($"Discord response: {(int)response.StatusCode} {response.StatusCode}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}

static void LoadLocalEnv(string path)
{
    try
    {
        if (!File.Exists(path))
            return;

        Console.WriteLine($"Loading local env from {path}...");
        var lines = File.ReadAllLines(path);

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (string.IsNullOrWhiteSpace(line)
                || line.StartsWith('#'))
            {
                continue;
            }

            var idx = line.IndexOf('=', StringComparison.Ordinal);
            if (idx <= 0)
            {
                continue;
            }

            var key = line[..idx].Trim();
            var value = line[(idx + 1)..].Trim();

            Environment.SetEnvironmentVariable(key, value);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Failed to load .env.local: " + ex.Message);
    }
}
