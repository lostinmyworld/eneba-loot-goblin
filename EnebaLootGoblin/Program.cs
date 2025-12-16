using EnebaLootGoblin.Abstractions;
using EnebaLootGoblin.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Social.Oversharers.Abstractions;

var services = new ServiceCollection();

services.AddDependencies();

var serviceProvider = services.BuildServiceProvider();

var parser = serviceProvider.GetRequiredService<IParser>();

var environmentVars = parser.GetEnvironmentVariables();


var apiClient = serviceProvider.GetRequiredService<IApiClient>();

var discordSharer = serviceProvider.GetRequiredService<IDiscordSharer>();

try
{
    Console.WriteLine("Downloading feed...");

    string csv = await apiClient.RetrieveCsv(environmentVars.EnebaFeedUrl!);

    var offers = parser.ParseOffers(csv);
    if (offers.Count == 0)
    {
        Console.WriteLine("No offers found with the specified discount.");
        return;
    }

    var discordRequest = parser.BuildDiscordRequest(offers);
    await discordSharer.SendToDiscord(discordRequest, environmentVars.DiscordWebHook!);

    Console.WriteLine("Finished.");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}

