using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnebaLootGoblin.Models;
using SocialModels;

namespace EnebaLootGoblin;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly EnvironmentVariables _environmentVariables;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public ApiClient(EnvironmentVariables environmentVariables)
    {
        _environmentVariables = environmentVariables;
        _httpClient = CreateEnebaClient(environmentVariables);
    }

    public async Task<string> RetrieveCsvAsync()
    {
        var enebaResponse = await _httpClient.GetAsync(_environmentVariables.EnebaFeedUrl);
        enebaResponse.EnsureSuccessStatusCode();

        using var stream = await enebaResponse.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        return await reader.ReadToEndAsync();
    }

    public async Task SendToDiscordAsync(DiscordRequest discordRequest)
    {
        Console.WriteLine("Posting to Discord...");
        var jsonPayload = JsonSerializer.Serialize(discordRequest, _jsonOptions);

        var content = new StringContent(
            jsonPayload,
            Encoding.UTF8,
            MediaTypeNames.Application.Json);
        using var discordClient = new HttpClient();

        var discordResponse = await discordClient.PostAsync(
            _environmentVariables.DiscordWebHook,
            content);

        Console.WriteLine($"Discord response: {(int)discordResponse.StatusCode} {discordResponse.StatusCode}");
    }

    private static HttpClient CreateEnebaClient(EnvironmentVariables environmentVariables)
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

        if (Uri.TryCreate(environmentVariables.EnebaFeedUrl, UriKind.Absolute, out var feedUri) && (feedUri.Scheme == "http" || feedUri.Scheme == "https"))
        {
            httpClient.DefaultRequestHeaders.Referrer = new Uri(feedUri.GetLeftPart(UriPartial.Authority));
        }

        return httpClient;
    }
}
