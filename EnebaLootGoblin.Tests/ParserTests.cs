using System.Globalization;
using EnebaLootGoblin.Abstractions;
using EnebaLootGoblin.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace EnebaLootGoblin.Tests;

public class ParserTests
{
    private const string Header =
        "id, title, original_title, description, link, image_link, availability, price, google_product_category, brand, region, condition, identifier_exists";

    private const string GameId = "steam-some-game-pc-steam-key-global";
    private const string GameTitle = "Some Game";

    private readonly IParser _parser;

    public ParserTests()
    {
        Environment.SetEnvironmentVariable("ENEBA_FEED_URL", "https://feed.test");
        Environment.SetEnvironmentVariable("DISCORD_WEBHOOK_URL", "https://webhook.test");
        Environment.SetEnvironmentVariable("MAX_PRICE", "20");
        Environment.SetEnvironmentVariable("MAX_OFFERS", "100");

        _parser = new ServiceCollection()
            .AddDependencies()
            .BuildServiceProvider()
            .GetRequiredService<IParser>();
    }

    [Fact]
    public void ParseOffers_PcGame_IsKept()
    {
        var offers = _parser.ParseOffers(BuildCsv());

        var offer = Assert.Single(offers);
        Assert.Equal(GameTitle, offer.Title);
        Assert.Equal(10.00m, offer.Price);
    }

    [Theory]
    [InlineData("steam-some-game-pc-steam-key-global")]
    [InlineData("gog-some-game-pc-gog-key-global")]
    [InlineData("epic-games-some-game-epic-games-key-global")]
    [InlineData("origin-some-game-origin-key-global")]
    [InlineData("ea-app-some-game-ea-app-key-global")]
    [InlineData("uplay-some-game-uplay-key-global")]
    [InlineData("ubisoft-some-game-ubisoft-connect-key-global")]
    [InlineData("bethesda-some-game-bethesda-net-key-global")]
    [InlineData("blizzard-some-game-battle-net-key-global")]
    [InlineData("rockstar-some-game-rockstar-launcher-key-global")]
    public void ParseOffers_PcStoreId_IsKept(string id)
    {
        var offers = _parser.ParseOffers(BuildCsv(id: id));

        Assert.Single(offers);
    }

    [Theory]
    [InlineData("xbox-some-game-xbox-live-key-europe")]
    [InlineData("psn-some-game-ps5-psn-key-europe")]
    [InlineData("nintendo-some-game-nintendo-switch-eshop-key-europe")]
    [InlineData("other-avast-premium-security-key-global")]
    [InlineData("")]
    public void ParseOffers_NonPcStoreId_IsSkipped(string id)
    {
        var offers = _parser.ParseOffers(BuildCsv(id: id));

        Assert.Empty(offers);
    }

    [Theory]
    [InlineData("steam-some-game-xbox-live-key-europe")]
    [InlineData("steam-some-game-psn-key-europe")]
    [InlineData("steam-some-game-ps4-key-europe")]
    [InlineData("steam-some-game-ps5-key-europe")]
    [InlineData("steam-some-game-nintendo-eshop-key-europe")]
    public void ParseOffers_ConsoleKeyUnderPcStoreId_IsSkipped(string id)
    {
        var offers = _parser.ParseOffers(BuildCsv(id: id));

        Assert.Empty(offers);
    }

    [Fact]
    public void ParseOffers_MacOnlyId_IsSkipped()
    {
        var offers = _parser.ParseOffers(BuildCsv(id: "steam-some-game-mac-steam-key-global"));

        Assert.Empty(offers);
    }

    [Fact]
    public void ParseOffers_PcAndMacId_IsKept()
    {
        var offers = _parser.ParseOffers(BuildCsv(id: "origin-some-game-pc-mac-origin-key-global"));

        Assert.Single(offers);
    }

    [Theory]
    [InlineData("steam-some-game-dlc-steam-key-global", GameTitle)]
    [InlineData("steam-some-game-expansion-pack-pc-steam-key-global", GameTitle)]
    [InlineData(GameId, "Some Game - Extra Pack (DLC)")]
    public void ParseOffers_Dlc_IsSkipped(string id, string title)
    {
        var offers = _parser.ParseOffers(BuildCsv(id: id, title: title));

        Assert.Empty(offers);
    }

    [Fact]
    public void ParseOffers_GameWithDlcsBundle_IsKept()
    {
        var offers = _parser.ParseOffers(BuildCsv(id: "steam-some-game-2-dlcs-steam-key-global", title: "Some Game + 2 DLCs"));

        Assert.Single(offers);
    }

    [Theory]
    [InlineData("steam-some-game-vr-pc-steam-key-global", GameTitle)]
    [InlineData(GameId, "Some Game [VR]")]
    [InlineData(GameId, "Some Game VR")]
    public void ParseOffers_Vr_IsSkipped(string id, string title)
    {
        var offers = _parser.ParseOffers(BuildCsv(id: id, title: title));

        Assert.Empty(offers);
    }

    [Theory]
    [InlineData("Some Antivirus 2025")]
    [InlineData("Some VPN 1 Year")]
    [InlineData("Panda Dome Essential 5 Devices 2 Year")]
    [InlineData("Some Security (1 Device, 2 Years)")]
    public void ParseOffers_SoftwareTitle_IsSkipped(string title)
    {
        var offers = _parser.ParseOffers(BuildCsv(title: title));

        Assert.Empty(offers);
    }

    [Fact]
    public void ParseOffers_GameTitleWithDeviceWord_IsKept()
    {
        var offers = _parser.ParseOffers(BuildCsv(title: "Nancy Drew: The Deadly Device"));

        Assert.Single(offers);
    }

    [Theory]
    [InlineData("Steam Wallet Gift Card 20 EUR")]
    [InlineData("Some Crypto Voucher")]
    public void ParseOffers_VoucherTitle_IsSkipped(string title)
    {
        var offers = _parser.ParseOffers(BuildCsv(title: title));

        Assert.Empty(offers);
    }

    [Theory]
    [InlineData("5.01 EUR", "5.01")]
    [InlineData("20.00 EUR", "20.00")]
    [InlineData("EUR 10,50", "10.50")]
    public void ParseOffers_PriceInRange_IsKeptWithParsedPrice(string rawPrice, string expectedPrice)
    {
        var offers = _parser.ParseOffers(BuildCsv(price: rawPrice));

        var offer = Assert.Single(offers);
        Assert.Equal(decimal.Parse(expectedPrice, CultureInfo.InvariantCulture), offer.Price);
    }

    [Theory]
    [InlineData("5.00 EUR")]
    [InlineData("20.01 EUR")]
    [InlineData("N/A")]
    public void ParseOffers_PriceOutOfRangeOrInvalid_IsSkipped(string rawPrice)
    {
        var offers = _parser.ParseOffers(BuildCsv(price: rawPrice));

        Assert.Empty(offers);
    }

    [Theory]
    [InlineData("313")]
    [InlineData("")]
    public void ParseOffers_NonGameCategory_IsSkipped(string category)
    {
        var offers = _parser.ParseOffers(BuildCsv(category: category));

        Assert.Empty(offers);
    }

    [Theory]
    [InlineData("out of stock")]
    [InlineData("preorder")]
    public void ParseOffers_NotInStock_IsSkipped(string availability)
    {
        var offers = _parser.ParseOffers(BuildCsv(availability: availability));

        Assert.Empty(offers);
    }

    [Theory]
    [InlineData("global")]
    [InlineData("europe")]
    [InlineData("EUROPE")]
    public void ParseOffers_AcceptableRegion_IsKept(string region)
    {
        var offers = _parser.ParseOffers(BuildCsv(region: region));

        Assert.Single(offers);
    }

    [Fact]
    public void ParseOffers_OtherRegion_IsSkipped()
    {
        var offers = _parser.ParseOffers(BuildCsv(region: "united_states"));

        Assert.Empty(offers);
    }

    private static string BuildCsv(
        string id = GameId,
        string title = GameTitle,
        string price = "10.00 EUR",
        string category = "1279",
        string availability = "in stock",
        string region = "global")
    {
        var row = $"{id},\"{title}\",\"{title}\",,https://www.eneba.com/{id},https://img.test/{id}.jpeg,\"{availability}\",\"{price}\",{category},Brand,{region},new,no";

        return $"{Header}\n{row}\n";
    }
}
