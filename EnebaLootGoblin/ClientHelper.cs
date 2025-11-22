using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;

namespace EnebaLootGoblin;

internal static class ClientHelper
{
    internal static async Task<string> RetrieveCsvAsync(string feedUrl)
    {
        using var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
        };

        using var enebaClient = new HttpClient(handler);

        enebaClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        enebaClient.DefaultRequestHeaders.Accept.Clear();
        enebaClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Text.Csv));
        enebaClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*") { Quality = 0.1 });
        enebaClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");

        if (Uri.TryCreate(feedUrl, UriKind.Absolute, out var feedUri) && (feedUri.Scheme == "http" || feedUri.Scheme == "https"))
        {
            enebaClient.DefaultRequestHeaders.Referrer = new Uri(feedUri.GetLeftPart(UriPartial.Authority));
        }

        return await ParseToCsvAsync(enebaClient, feedUrl);
    }

    internal static async Task<string> ParseToCsvAsync(HttpClient enebaClient, string feedUrl)
    {
        var enebaResponse = await enebaClient.GetAsync(feedUrl);
        enebaResponse.EnsureSuccessStatusCode();

        using var stream = await enebaResponse.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true);

        return await reader.ReadToEndAsync();
    }

    internal static void LoadLocalEnv(string path)
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
}
