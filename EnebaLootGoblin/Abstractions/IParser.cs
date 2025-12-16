using EnebaLootGoblin.Models;
using Social.Models.Discord;

namespace EnebaLootGoblin.Abstractions;

public interface IParser
{
    EnvironmentVariables GetEnvironmentVariables();
    List<Offer> ParseOffers(string csv);
    DiscordRequest BuildDiscordRequest(List<Offer> offers);
}
