using BazosScraper;
using Spectre.Console;
using Spectre.Console.Rendering;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ── Header ────────────────────────────────────────────────────────────────────
AnsiConsole.Write(
    new FigletText("Bazos")
        .LeftJustified()
        .Color(Color.DodgerBlue1));
AnsiConsole.Write(
    new Rule("[dim]bazos.cz / bazos.sk scraper[/]")
        .RuleStyle("grey dim")
        .LeftJustified());
AnsiConsole.WriteLine();

// ── Search term ───────────────────────────────────────────────────────────────
string searchTerm;
if (args.Length > 0)
{
    searchTerm = string.Join(" ", args);
    AnsiConsole.MarkupLine($"[grey]Search term:[/] [bold cyan]{Markup.Escape(searchTerm)}[/]");
    AnsiConsole.WriteLine();
}
else
{
    searchTerm = AnsiConsole.Prompt(
        new TextPrompt<string>("[bold]Search term:[/]")
            .PromptStyle("cyan")
            .ValidationErrorMessage("[red]Please enter a search term.[/]")
            .Validate(s => !string.IsNullOrWhiteSpace(s)
                ? ValidationResult.Success()
                : ValidationResult.Error("[red]Search term cannot be empty.[/]")));
    AnsiConsole.WriteLine();
}

// ── Optional filters ──────────────────────────────────────────────────────────
AnsiConsole.MarkupLine("[dim]Optional filters — press Enter to skip[/]");
AnsiConsole.WriteLine();

var priceFromInput = AnsiConsole.Prompt(
    new TextPrompt<string>("[grey]Price from (CZK/EUR):[/]")
        .PromptStyle("yellow")
        .AllowEmpty());

var priceToInput = AnsiConsole.Prompt(
    new TextPrompt<string>("[grey]Price to   (CZK/EUR):[/]")
        .PromptStyle("yellow")
        .AllowEmpty());

var locationZipInput = AnsiConsole.Prompt(
    new TextPrompt<string>("[grey]Location ZIP:        [/]")
        .PromptStyle("yellow")
        .AllowEmpty());

int radiusKm = 0;
if (!string.IsNullOrWhiteSpace(locationZipInput))
{
    radiusKm = AnsiConsole.Prompt(
        new TextPrompt<int>("[grey]Radius (km):         [/]")
            .PromptStyle("yellow")
            .DefaultValue(25));
}

AnsiConsole.WriteLine();

var options = new SearchOptions
{
    SearchTerm = searchTerm,
    PriceFrom = string.IsNullOrWhiteSpace(priceFromInput) ? null : priceFromInput,
    PriceTo = string.IsNullOrWhiteSpace(priceToInput) ? null : priceToInput,
    LocationZip = string.IsNullOrWhiteSpace(locationZipInput) ? null : locationZipInput,
    RadiusKm = radiusKm
};

using var scraper = new BazosScraperService();

var sites = new[] { "https://www.bazos.cz", "https://www.bazos.sk" };

// ── Search each site ──────────────────────────────────────────────────────────
foreach (var site in sites)
{
    AnsiConsole.Write(new Rule($"[bold cyan]{Markup.Escape(site)}[/]").RuleStyle("cyan"));
    AnsiConsole.WriteLine();

    List<Ad> ads = [];

    await AnsiConsole.Status()
        .Spinner(Spinner.Known.Dots)
        .SpinnerStyle(Style.Parse("cyan"))
        .StartAsync("Connecting...", async ctx =>
        {
            ads = await scraper.SearchAllPagesAsync(site, options, msg =>
            {
                ctx.Status = Markup.Escape(msg);
            });
        });

    if (ads.Count == 0)
    {
        AnsiConsole.MarkupLine("[yellow]  No ads found.[/]");
        AnsiConsole.WriteLine();
        continue;
    }

    AnsiConsole.MarkupLine($"[green]  Found {ads.Count} ad(s)[/]");
    AnsiConsole.WriteLine();

    var table = new Table()
        .Border(TableBorder.Rounded)
        .BorderStyle(Style.Parse("grey"))
        .Expand()
        .AddColumn(new TableColumn("[grey]#[/]").RightAligned().NoWrap())
        .AddColumn(new TableColumn("[bold]Title[/]"))
        .AddColumn(new TableColumn("[bold]Price[/]").NoWrap())
        .AddColumn(new TableColumn("[bold]Location[/]").NoWrap())
        .AddColumn(new TableColumn("[bold]Date[/]").NoWrap());

    int i = 1;
    foreach (var ad in ads)
    {
        // Build the title cell as stacked renderables to avoid URL escaping issues in markup
        var titleCellItems = new List<IRenderable>
        {
            (IRenderable)new Markup($"[bold]{Markup.Escape(ad.Title)}[/]"),
            new Text(TruncateUrl(ad.Url, 60),
                new Style(Color.Blue, decoration: Decoration.Dim, link: ad.Url))
        };
        if (!string.IsNullOrEmpty(ad.Description))
            titleCellItems.Add(new Markup($"[dim]{Markup.Escape(ad.Description)}[/]"));

        table.AddRow(
            new Markup($"[grey]{i++}[/]"),
            new Rows(titleCellItems),
            string.IsNullOrEmpty(ad.Price)
                ? (IRenderable)new Markup("[grey]-[/]")
                : new Markup($"[green]{Markup.Escape(ad.Price)}[/]"),
            string.IsNullOrEmpty(ad.Location)
                ? (IRenderable)new Markup("[grey]-[/]")
                : new Text(ad.Location),
            string.IsNullOrEmpty(ad.Date)
                ? (IRenderable)new Markup("[grey]-[/]")
                : new Markup($"[blue]{Markup.Escape(ad.Date)}[/]"));
    }

    AnsiConsole.Write(table);
    AnsiConsole.WriteLine();
}

// ── Footer ────────────────────────────────────────────────────────────────────
AnsiConsole.Write(new Rule("[dim]Done[/]").RuleStyle("grey dim"));
AnsiConsole.Markup("[dim]Press any key to exit...[/]");
Console.ReadKey();

static string TruncateUrl(string url, int max) =>
    url.Length > max ? url[..max] + "\u2026" : url;
