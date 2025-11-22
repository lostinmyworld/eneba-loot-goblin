namespace EnebaLootGoblin;

internal record Offer(
    string Title,
    decimal Price,
    string ImageUrl,
    string Url,
    bool IsAvailable,
    int CategoryId);
