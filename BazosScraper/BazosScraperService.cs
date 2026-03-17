using System.Web;
using HtmlAgilityPack;

namespace BazosScraper;

public class BazosScraperService : IDisposable
{
    private readonly HttpClient _http;
    private const int PageSize = 20;
    private const int RequestDelayMs = 600;

    public BazosScraperService()
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            UseCookies = true
        };

        _http = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        // Mimic a real browser to avoid blocks
        _http.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
            "(KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
        _http.DefaultRequestHeaders.Add("Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        _http.DefaultRequestHeaders.Add("Accept-Language", "cs,sk;q=0.9,en;q=0.8");
    }

    /// <summary>
    /// Fetches all pages of search results for the given site and options.
    /// </summary>
    public async Task<List<Ad>> SearchAllPagesAsync(string baseUrl, SearchOptions options)
    {
        var allAds = new List<Ad>();
        int page = 0;
        int? totalAds = null;

        while (true)
        {
            var url = BuildSearchUrl(baseUrl, options, page * PageSize);
            Console.WriteLine($"  Fetching page {page + 1}: {url}");

            (List<Ad> pageAds, int? pageTotalCount) result;
            try
            {
                result = await FetchPageAsync(baseUrl, url);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error fetching page {page + 1}: {ex.Message}");
                break;
            }

            var (pageAds, pageTotalCount) = result;

            if (page == 0 && pageTotalCount.HasValue)
            {
                totalAds = pageTotalCount.Value;
                Console.WriteLine($"  Total ads found: {totalAds}");
            }

            if (pageAds.Count == 0)
                break;

            allAds.AddRange(pageAds);
            Console.WriteLine($"  Got {pageAds.Count} ads (total so far: {allAds.Count})");

            // Stop when we have fetched all known ads, trimming any over-fetch
            if (totalAds.HasValue && allAds.Count >= totalAds.Value)
            {
                if (allAds.Count > totalAds.Value)
                    allAds.RemoveRange(totalAds.Value, allAds.Count - totalAds.Value);
                break;
            }

            // If we got fewer than a full page, there's no next page
            if (pageAds.Count < PageSize)
                break;

            page++;
            await Task.Delay(RequestDelayMs);
        }

        return allAds;
    }

    private string BuildSearchUrl(string baseUrl, SearchOptions options, int start)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["hledat"] = options.SearchTerm;
        query["rubriky"] = "www";
        query["hlokalita"] = options.LocationZip ?? string.Empty;
        query["humkreis"] = options.RadiusKm > 0 ? options.RadiusKm.ToString() : "25";
        query["cenaod"] = options.PriceFrom ?? string.Empty;
        query["cenado"] = options.PriceTo ?? string.Empty;
        query["Submit"] = "Hledat";
        query["kitx"] = "ano";
        if (start > 0)
            query["start"] = start.ToString();

        return $"{baseUrl}/search.php?{query}";
    }

    private async Task<(List<Ad> ads, int? totalCount)> FetchPageAsync(string baseUrl, string url)
    {
        var response = await _http.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        return (ParseAds(doc, baseUrl), ParseTotalCount(doc));
    }

    private static int? ParseTotalCount(HtmlDocument doc)
    {
        var node = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'inzeratynadpis')]");
        if (node == null) return null;

        var text = HtmlEntity.DeEntitize(node.InnerText);
        var match = System.Text.RegularExpressions.Regex.Match(text, @"z\s+(\d+)");
        if (!match.Success) return null;

        return int.TryParse(match.Groups[1].Value, out var n) ? n : null;
    }

    private static List<Ad> ParseAds(HtmlDocument doc, string baseUrl)
    {
        var ads = new List<Ad>();

        // Bazos ad structure: <div class="inzeraty inzeratyflex"> (exact class = "inzeraty inzeratyflex")
        // The header row uses "listainzerat inzeratyflex" — we skip it by requiring h2.nadpis inside
        var adNodes = doc.DocumentNode.SelectNodes("//div[contains(@class,'inzeratyflex')]");

        if (adNodes == null || adNodes.Count == 0)
            return ads;

        foreach (var node in adNodes)
        {
            var ad = ParseAdNode(node, baseUrl);
            if (ad != null)
                ads.Add(ad);
        }

        return ads;
    }

    private static Ad? ParseAdNode(HtmlNode node, string baseUrl)
    {
        // Title link is inside <h2 class="nadpis"> > <a>
        // (bazos uses unquoted class attr in HTML, HtmlAgilityPack normalises it)
        var titleLink = node.SelectSingleNode(".//h2[contains(@class,'nadpis')]//a")
                      ?? node.SelectSingleNode(".//h2//a");

        if (titleLink == null)
            return null;

        var title = HtmlEntity.DeEntitize(titleLink.InnerText).Trim();
        if (string.IsNullOrWhiteSpace(title))
            return null;

        var href = titleLink.GetAttributeValue("href", string.Empty);
        if (string.IsNullOrEmpty(href))
            return null;
        var adUrl = href.StartsWith("http") ? href : baseUrl.TrimEnd('/') + "/" + href.TrimStart('/');

        // Date — in <span class="velikost10"> e.g. " - [17.3. 2026]"
        var dateNode = node.SelectSingleNode(".//*[contains(@class,'velikost10')]");
        var date = dateNode != null
            ? System.Text.RegularExpressions.Regex.Match(
                HtmlEntity.DeEntitize(dateNode.InnerText), @"\[([^\]]+)\]").Groups[1].Value
            : string.Empty;

        // Price — <div class="inzeratycena"><b><span translate="no">PRICE</span></b></div>
        var priceNode = node.SelectSingleNode(".//div[contains(@class,'inzeratycena')]//span[@translate]")
                      ?? node.SelectSingleNode(".//div[contains(@class,'inzeratycena')]");
        var price = priceNode != null
            ? HtmlEntity.DeEntitize(priceNode.InnerText).Trim()
            : string.Empty;
        price = System.Text.RegularExpressions.Regex.Replace(price, @"\s+", " ").Trim();

        // Location — <div class="inzeratylok">City<br>ZIP</div>
        var localityNode = node.SelectSingleNode(".//div[contains(@class,'inzeratylok')]");
        var location = localityNode != null
            ? HtmlEntity.DeEntitize(localityNode.InnerText)
                .Replace("\r", " ").Replace("\n", " ").Trim()
            : string.Empty;
        location = System.Text.RegularExpressions.Regex.Replace(location, @"\s+", " ").Trim();

        // Description — <div class="popis">...</div>
        var descNode = node.SelectSingleNode(".//div[contains(@class,'popis')]");
        var description = descNode != null
            ? HtmlEntity.DeEntitize(descNode.InnerText).Trim()
            : string.Empty;
        description = System.Text.RegularExpressions.Regex.Replace(description, @"\s+", " ").Trim();

        return new Ad
        {
            Title = title,
            Price = price,
            Location = location,
            Date = date,
            Url = adUrl,
            Description = description.Length > 200 ? description[..200] + "…" : description
        };
    }

    public void Dispose() => _http.Dispose();
}
