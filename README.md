# Bazos Scraper

A .NET 10.0 command-line application that searches classified ads on [bazos.cz](https://bazos.cz) and [bazos.sk](https://bazos.sk) — the most popular Czech and Slovak marketplaces. Results are displayed in a rich, colour-formatted table directly in your terminal.

## Features

- Search across both bazos.cz and bazos.sk at the same time
- Optional filters: minimum price, maximum price, location ZIP code, and search radius
- Automatic pagination — fetches every page of results, not just the first one
- Rate-limiting (600 ms delay between requests) to avoid being blocked
- Rich terminal output powered by [Spectre.Console](https://spectreconsole.net/)

## Prerequisites

| Requirement | Version |
|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0 or later |

Verify your installation:

```bash
dotnet --version
```

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/davidnemec1993/bazos-scraper.git
cd bazos-scraper
```

### 2. Restore dependencies

```bash
dotnet restore
```

### 3. Build the project

```bash
dotnet build scraper.sln
```

### 4. Run the application

**Interactive mode** — the application will prompt you for a search term and optional filters:

```bash
dotnet run --project BazosScraper/BazosScraper.csproj
```

**Command-line argument mode** — pass the search term directly to skip the first prompt:

```bash
dotnet run --project BazosScraper/BazosScraper.csproj -- "used laptop"
```

### 5. Follow the prompts

After entering the search term you will be asked for optional filters (press **Enter** to skip any):

```
Price from (CZK/EUR):
Price to (CZK/EUR):
Location ZIP code:
Radius in km (only if ZIP provided):
```

The application then searches both sites and prints a table of matching ads.

## Example Output

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                           bazos.cz — used laptop                             │
├──────────────────────────────┬──────────┬─────────────┬────────────┬─────────┤
│ Title                        │ Price    │ Location    │ Date       │ URL     │
├──────────────────────────────┼──────────┼─────────────┼────────────┼─────────┤
│ Lenovo ThinkPad T480         │ 8 500 Kč │ Praha 10000 │ 19.03.2026 │ https://│
│ HP EliteBook 840 G5          │ 7 200 Kč │ Brno 60200  │ 18.03.2026 │ https://│
└──────────────────────────────┴──────────┴─────────────┴────────────┴─────────┘
```

## Project Structure

```
bazos-scraper/
├── scraper.sln                  # Root solution file
└── BazosScraper/
    ├── BazosScraper.csproj      # Project configuration and NuGet dependencies
    ├── Program.cs               # Entry point — CLI prompts and output rendering
    ├── BazosScraperService.cs   # HTTP requests, HTML parsing, pagination logic
    └── Models.cs                # Ad and SearchOptions data models
```

## Dependencies

| Package | Version | Purpose |
|---|---|---|
| [HtmlAgilityPack](https://html-agility-pack.net/) | 1.12.4 | HTML parsing |
| [Spectre.Console](https://spectreconsole.net/) | 0.54.0 | Rich terminal UI |

## License

This project is provided as-is for educational and personal use.
