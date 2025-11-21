namespace EnebaLootGoblin;

internal record Offer(
    string Title,
    double Price,
    double OldPrice,
    int Discount,
    string ImageUrl,
    string Url);
