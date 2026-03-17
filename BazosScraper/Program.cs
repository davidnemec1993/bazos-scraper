using BazosScraper;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("=== Bazos.cz / Bazos.sk Scraper ===");
Console.WriteLine();

string searchTerm;
if (args.Length > 0)
{
    searchTerm = string.Join(" ", args);
}
else
{
    Console.Write("Enter search term: ");
    searchTerm = Console.ReadLine()?.Trim() ?? string.Empty;
}

if (string.IsNullOrWhiteSpace(searchTerm))
{
    Console.WriteLine("No search term provided. Exiting.");
    return;
}

string? priceFrom = null, priceTo = null, locationZip = null;
int radiusKm = 0;

Console.Write("Price from (CZK/EUR, leave empty to skip): ");
var input = Console.ReadLine()?.Trim();
if (!string.IsNullOrEmpty(input)) priceFrom = input;

Console.Write("Price to (CZK/EUR, leave empty to skip): ");
input = Console.ReadLine()?.Trim();
if (!string.IsNullOrEmpty(input)) priceTo = input;

Console.Write("Location ZIP code (leave empty to skip): ");
input = Console.ReadLine()?.Trim();
if (!string.IsNullOrEmpty(input))
{
    locationZip = input;
    Console.Write("Search radius in km (default 0 = exact match): ");
    input = Console.ReadLine()?.Trim();
    if (!string.IsNullOrEmpty(input) && int.TryParse(input, out var r)) radiusKm = r;
}

var options = new SearchOptions
{
    SearchTerm = searchTerm,
    PriceFrom = priceFrom,
    PriceTo = priceTo,
    LocationZip = locationZip,
    RadiusKm = radiusKm
};

using var scraper = new BazosScraperService();

var sites = new[] { "https://www.bazos.cz", "https://www.bazos.sk" };

foreach (var baseUrl in sites)
{
    Console.WriteLine();
    Console.WriteLine($"--- Searching {baseUrl} ---");
    Console.WriteLine();

    var ads = await scraper.SearchAllPagesAsync(baseUrl, options);

    if (ads.Count == 0)
    {
        Console.WriteLine("No ads found.");
        continue;
    }

    Console.WriteLine($"Found {ads.Count} ads:");
    Console.WriteLine(new string('-', 80));

    foreach (var ad in ads)
    {
        Console.WriteLine($"Title    : {ad.Title}");
        Console.WriteLine($"Price    : {ad.Price}");
        Console.WriteLine($"Location : {ad.Location}");
        Console.WriteLine($"Date     : {ad.Date}");
        Console.WriteLine($"URL      : {ad.Url}");
        if (!string.IsNullOrEmpty(ad.Description))
            Console.WriteLine($"Desc     : {ad.Description}");
        Console.WriteLine(new string('-', 80));
    }
}

Console.WriteLine();
Console.WriteLine("Done. Press any key to exit.");
Console.ReadKey();
