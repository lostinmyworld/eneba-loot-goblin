using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace EnebaLootGoblin;

internal static class Parser
{
    private const string TitleField = "title";
    private const string PriceField = "price";
    private const string OldPriceField = "original_price";
    private const string ImageField = "image";
    private const string UrlField = "deeplink";

    internal static List<Offer> ParseOffers(string csv, int minDiscount)
    {
        var offers = new List<Offer>();

        if (string.IsNullOrWhiteSpace(csv))
        {
            return offers;
        }

        using var reader = new StringReader(csv);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim,
            PrepareHeaderForMatch = args => args.Header?.Trim() ?? string.Empty,
        };

        using var csvReader = new CsvReader(reader, config);
        try
        {
            if (!csvReader.Read() || !csvReader.ReadHeader())
            {
                return offers;
            }

            var headers = csvReader.Context.Reader?.HeaderRecord ?? [];
            var hasTitle = Array.Exists(headers, h => h.Equals(TitleField, StringComparison.OrdinalIgnoreCase));
            var hasPrice = Array.Exists(headers, h => h.Equals(PriceField, StringComparison.OrdinalIgnoreCase));
            var hasOldPrice = Array.Exists(headers, h => h.Equals(OldPriceField, StringComparison.OrdinalIgnoreCase));
            if (!hasTitle || !hasPrice || !hasOldPrice)
            {
                Console.WriteLine("CSV header is missing required fields.");
                return offers;
            }

            while (csvReader.Read())
            {
                var title = csvReader.GetField(TitleField)?.Trim() ?? string.Empty;
                var priceStr = (csvReader.GetField(PriceField)?.Trim() ?? string.Empty)
                    .Replace(",", ".");
                var oldPriceStr = (csvReader.GetField(OldPriceField)?.Trim() ?? string.Empty)
                    .Replace(",", ".");

                if (!double.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var price)
                    || !double.TryParse(oldPriceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var oldPrice)
                    || oldPrice <= 0
                    || price <= 0)
                {
                    continue;
                }

                var discount = (int)Math.Round((oldPrice - price) / oldPrice * 100);
                if (discount < minDiscount)
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
                    oldPrice,
                    discount,
                    imageUrl,
                    url));
            }
        }
        catch (HeaderValidationException ex)
        {
            Console.WriteLine($"CSV header validation failed. Exception: {ex.Message}");
        }

        return offers;
    }
}
