namespace EnebaLootGoblin.Abstractions;

public interface IApiClient
{
    Task<string> RetrieveCsv(string enebaFeedUrl);
}
