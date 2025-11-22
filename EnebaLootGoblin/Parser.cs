using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;

namespace EnebaLootGoblin;

internal static class Parser
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

    internal static List<Offer> ParseOffers(
        string csv,
        decimal minPrice,
        int maxOffers)
    {
        var offers = new List<Offer>();

        if (string.IsNullOrWhiteSpace(csv))
        {
            return offers;
        }

        using var reader = new StringReader(csv);
        using var csvReader = new CsvReader(reader, _csvConfig);

        try
        {
            if (!csvReader.Read() || !csvReader.ReadHeader())
            {
                return offers;
            }

            var headers = csvReader.Context.Reader?.HeaderRecord ?? Array.Empty<string>();
            var hasTitle = Array.Exists(headers, h => h.Equals(TitleField, StringComparison.OrdinalIgnoreCase));
            var hasPrice = Array.Exists(headers, h => h.Equals(PriceField, StringComparison.OrdinalIgnoreCase));
            if (!hasTitle || !hasPrice)
            {
                Console.WriteLine("CSV header is missing required fields.");
                return offers;
            }

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
                    || price > minPrice
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
        }
        catch (HeaderValidationException ex)
        {
            Console.WriteLine($"CSV header validation failed. Exception: {ex.Message}");
        }

        return FilterRandomOffers(offers, maxOffers);
    }

    private static List<Offer> FilterRandomOffers(List<Offer> offers, int maxOffers)
    {
        var count = Math.Min(maxOffers, offers.Count);
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
}
