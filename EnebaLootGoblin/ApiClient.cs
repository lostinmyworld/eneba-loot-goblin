using System.Net;
using System.Net.Mime;
using System.Text;
using EnebaLootGoblin.Abstractions;

namespace EnebaLootGoblin;

public class ApiClient : IApiClient
{
    public async Task<string> RetrieveCsv(string enebaFeedUrl)
    {
        if (!enebaFeedUrl!.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return await File.ReadAllTextAsync(enebaFeedUrl);
        }

        using var httpClient = CreateEnebaClient(enebaFeedUrl);

        using var enebaResponse = await httpClient.GetAsync(enebaFeedUrl);
        enebaResponse.EnsureSuccessStatusCode();

        using var stream = await enebaResponse.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        return await reader.ReadToEndAsync();
    }

    private static HttpClient CreateEnebaClient(string enebaFeedUrl)
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
        };

        var httpClient = new HttpClient(handler);

        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(new(MediaTypeNames.Text.Csv));
        httpClient.DefaultRequestHeaders.Accept.Add(new("*/*") { Quality = 0.1 });
        httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");

        if (Uri.TryCreate(enebaFeedUrl, UriKind.Absolute, out var feedUri) && (feedUri.Scheme == "http" || feedUri.Scheme == "https"))
        {
            httpClient.DefaultRequestHeaders.Referrer = new Uri(feedUri.GetLeftPart(UriPartial.Authority));
        }

        return httpClient;
    }
}
