namespace BazosScraper;

public class Ad
{
    public string Title { get; set; } = string.Empty;
    public string Price { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class SearchOptions
{
    public string SearchTerm { get; set; } = string.Empty;
    public string? PriceFrom { get; set; }
    public string? PriceTo { get; set; }
    public string? LocationZip { get; set; }
    public int RadiusKm { get; set; } = 0;
}
