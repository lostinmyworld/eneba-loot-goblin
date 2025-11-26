using EnebaLootGoblin;
using File = System.IO.File;

var parser = new Parser();

var environmentVariables = parser.GetEnvironmentVariables();

var apiClient = new ApiClient(environmentVariables);

try
{
    Console.WriteLine("Downloading feed...");

    string csv = environmentVariables.EnebaFeedUrl!.StartsWith("http", StringComparison.OrdinalIgnoreCase)
        ? await apiClient.RetrieveCsvAsync()
        : await File.ReadAllTextAsync(environmentVariables.EnebaFeedUrl);

    var offers = parser.ParseOffers(csv);
    if (offers.Count == 0)
    {
        Console.WriteLine("No offers found with the specified discount.");
        return;
    }

    var discordRequest = parser.BuildDiscordRequest(offers);
    await apiClient.SendToDiscordAsync(discordRequest);

    Console.WriteLine("Finished.");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}

