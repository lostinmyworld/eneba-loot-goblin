using System.Net.Mime;
using System.Text;
using EnebaLootGoblin;
using File = System.IO.File;

ClientHelper.LoadLocalEnv(".env.local");

var feedUrl = Environment.GetEnvironmentVariable("ENEBA_FEED_URL")
    ?? throw new InvalidOperationException("'ENEBA_FEED_URL' is not set.");

var webhookUrl = Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_URL")
    ?? throw new InvalidOperationException("'DISCORD_WEBHOOK_URL' is not set.");

var minPrice = decimal.TryParse(Environment.GetEnvironmentVariable("MIN_PRICE"), out var minPriceValue)
    ? minPriceValue
    : 20;

var maxOffers = int.TryParse(Environment.GetEnvironmentVariable("MAX_OFFERS"), out var maxOffersValue)
    ? maxOffersValue
    : 3;

try
{
    Console.WriteLine("Downloading feed...");

    string csv = feedUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
        ? await ClientHelper.RetrieveCsvAsync(feedUrl)
        : await File.ReadAllTextAsync(feedUrl);

    var offers = Parser.ParseOffers(csv, minPrice, maxOffers);
    if (offers.Count == 0)
    {
        Console.WriteLine("No offers found with the specified discount.");
        return;
    }

    var payloadJson = DiscordBuilder.BuildDiscordPayload(offers, minPrice);

    Console.WriteLine("Posting to Discord...");

    var content = new StringContent(
        payloadJson,
        Encoding.UTF8,
        MediaTypeNames.Application.Json);
    using var discordClient = new HttpClient();

    var discordResponse = await discordClient.PostAsync(
        webhookUrl,
        content);

    Console.WriteLine($"Discord response: {(int)discordResponse.StatusCode} {discordResponse.StatusCode}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}

