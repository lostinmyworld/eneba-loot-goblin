using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using EnebaLootGoblin.Models;
using Social.Models.Discord;
using Social.Oversharers.Abstractions;
using IParser = EnebaLootGoblin.Abstractions.IParser;

namespace EnebaLootGoblin;

public class Parser : IParser
{
    private const string TitleField = "original_title";
    private const string PriceField = "price";
    private const string AvailabilityField = "availability";
    private const string CategoryField = "google_product_category";
    private const string RegionField = "region";
    private const string ImageField = "image_link";
    private const string UrlField = "link";

    private const int GamesCategoryId = 1279;
    private const bool IncludeDlc = false;
    private const bool IncludeVr = false;

    private static readonly HashSet<string> _acceptableRegions =
    [
        "europe",
        "global",
    ];

    private static readonly CsvConfiguration _csvConfig = new(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = true,
        IgnoreBlankLines = true,
        TrimOptions = TrimOptions.Trim,
        PrepareHeaderForMatch = args => args.Header?.Trim() ?? string.Empty,
    };

    private readonly EnvironmentVariables _environmentVariables;

    public Parser(IEnvironmentLoader environmentLoader)
    {
        ArgumentNullException.ThrowIfNull(environmentLoader);

        _environmentVariables = LoadEnvironmentVariables();
    }

    public EnvironmentVariables GetEnvironmentVariables()
    {
        return _environmentVariables;
    }

    public List<Offer> ParseOffers(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return [];
        }

        using var reader = new StringReader(csv);
        using var csvReader = new CsvReader(reader, _csvConfig);

        try
        {
            var areCsvHeadersValid = AreCsvHeadersValid(csvReader);
            if (!areCsvHeadersValid)
            {
                Console.WriteLine("CSV header is missing required fields.");

                return [];
            }

            var offers = new List<Offer>();

            while (csvReader.Read())
            {
                var title = csvReader.GetField(TitleField)?.Trim() ?? string.Empty;
                var rawPrice = csvReader.GetField(PriceField)?.Trim() ?? string.Empty;
                var region = csvReader.GetField(RegionField)?.Trim().ToLowerInvariant() ?? string.Empty;

                // Extract first numeric token (handles "5.27 EUR", "EUR 5,27", "€5,27", etc.)
                var match = Regex.Match(rawPrice, @"[-+]?\d+([.,]\d+)?");
                var priceStr = match.Success
                    ? match.Value.Replace(",", ".")
                    : string.Empty;

                var categoryStr = csvReader.GetField(CategoryField)?.Trim() ?? string.Empty;

                var availabilityStr = csvReader.GetField(AvailabilityField)?.Trim().ToLowerInvariant() ?? string.Empty;
                var isAvailable = availabilityStr == "in stock";

                if (!decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var price)
                    || price <= 0
                    || price > _environmentVariables.MaxPrice
                    || !int.TryParse(categoryStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var categoryId)
                    || categoryId != GamesCategoryId
                    || !isAvailable
                    || (!IncludeDlc && title.Contains("(DLC)")
                    || (!IncludeVr && title.Contains(" VR")))
                    || _acceptableRegions.Contains(region))
                {
                    continue;
                }

                var imageUrl = csvReader.TryGetField(ImageField, out string? imgField)
                    ? imgField?.Trim() ?? string.Empty
                    : string.Empty;
                var url = csvReader.TryGetField(UrlField, out string? urlField)
                    ? urlField ?? string.Empty
                    : string.Empty;

                offers.Add(new Offer(
                    title,
                    price,
                    imageUrl,
                    url,
                    isAvailable,
                    categoryId));
            }

            return FilterRandomOffers(offers);
        }
        catch (HeaderValidationException ex)
        {
            Console.Error.WriteLine($"CSV header validation failed. Exception: {ex.Message}");
        }

        return [];
    }

    public DiscordRequest BuildDiscordRequest(List<Offer> offers)
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
            ["title"] = $"Χαμηλές τιμές στην Eneba - κάτω των {_environmentVariables.MaxPrice} €",
            ["description"] = description,
        };

        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            embed["image"] = new { url = imageUrl };
        }

        return new()
        {
            Embeds = [embed],
        };
    }

    private static EnvironmentVariables LoadEnvironmentVariables()
    {
        var enebaFeedUrl = Environment.GetEnvironmentVariable("ENEBA_FEED_URL")
            ?? throw new InvalidOperationException("'ENEBA_FEED_URL' is not set.");

        var discordWebhookUrl = Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_URL")
            ?? throw new InvalidOperationException("'DISCORD_WEBHOOK_URL' is not set.");

        var maxPrice = decimal.TryParse(Environment.GetEnvironmentVariable("MAX_PRICE"), out var minPriceValue)
            ? minPriceValue
            : 20;

        var maxOffers = int.TryParse(Environment.GetEnvironmentVariable("MAX_OFFERS"), out var maxOffersValue)
            ? maxOffersValue
            : 3;

        return new(
            enebaFeedUrl,
            discordWebhookUrl,
            maxPrice,
            maxOffers);
    }

    private List<Offer> FilterRandomOffers(List<Offer> offers)
    {
        var count = Math.Min(_environmentVariables.MaxOffers, offers.Count);
        if (count == 0)
        {
            return [];
        }

        var tmpOffers = offers.ToArray();

        // Partial Fisher–Yates: for i in [0, count), pick j in [i, n) and swap
        for (int i = 0; i < count; i++)
        {
            int j = RandomNumberGenerator.GetInt32(i, offers.Count);
            (tmpOffers[i], tmpOffers[j]) = (tmpOffers[j], tmpOffers[i]);
        }

        var randomOffers = new List<Offer>(count);
        for (int i = 0; i < count; i++)
        {
            randomOffers.Add(tmpOffers[i]);
        }

        return randomOffers.OrderBy(offer => offer.Price)
            .ToList();
    }

    private static bool AreCsvHeadersValid(CsvReader csvReader)
    {
        if (!csvReader.Read() || !csvReader.ReadHeader())
        {
            return false;
        }

        var headers = csvReader.Context.Reader?.HeaderRecord ?? [];
        var hasTitle = Array.Exists(headers, h => h.Equals(TitleField, StringComparison.OrdinalIgnoreCase));
        var hasPrice = Array.Exists(headers, h => h.Equals(PriceField, StringComparison.OrdinalIgnoreCase));

        return hasTitle && hasPrice;
    }
}
