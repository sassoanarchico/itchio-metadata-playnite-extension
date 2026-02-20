<div align="center">

# itch.io Metadata Provider

**Playnite extension that fetches game metadata from itch.io via web scraping**

[![Version](https://img.shields.io/badge/version-1.1.0-blue.svg)]()
[![Platform](https://img.shields.io/badge/platform-Playnite%2010+-purple.svg)]()
[![Framework](https://img.shields.io/badge/.NET-Framework%204.8-green.svg)]()
[![License](https://img.shields.io/badge/license-MIT-lightgrey.svg)](LICENSE)

Retrieve comprehensive game metadata from itch.io pages — title, description, cover art, screenshots, tags, genres, developer info, release dates, and ratings — all directly in Playnite.

</div>

---

## Table of Contents

- [Features](#features)
- [Screenshots](#screenshots)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Settings](#settings)
- [Project Structure](#project-structure)
- [Development](#development)
- [Roadmap](#roadmap)
- [Known Issues](#known-issues)
- [Changelog](#changelog)
- [License](#license)

---

## Features

### Metadata Retrieval
| Feature | Description |
|---------|-------------|
| **Complete Metadata** | Title, description, cover art, screenshots, tags, genres, developer, publisher, release date, ratings |
| **Cover Images** | High-quality cover art automatically downloaded from game pages |
| **Screenshots** | Multiple screenshots available as background images |
| **Developer Info** | Developer and publisher information extracted from the page |
| **Tags & Genres** | Automatic tag extraction with smart genre mapping |
| **Release Dates** | Parsed from game page with multi-format support |
| **Ratings** | Community ratings converted to Playnite's 0–100 scale |
| **Additional Links** | itch.io page, developer website, social media, Steam links, and more |

### Search & Selection
| Feature | Description |
|---------|-------------|
| **Automatic Search** | Searches by game name if no itch.io link is present |
| **Interactive Selection** | Dialog window to select the correct game from multiple results |
| **Image Preview** | Visual preview of games in search results |
| **Auto-select First** | Option to automatically use the first search result |
| **Result Limit** | Configurable maximum search results (1–100) |

### Smart Recognition
| Feature | Description |
|---------|-------------|
| **Link Detection** | Automatically recognizes games with existing itch.io links |
| **Source Recognition** | Supports games imported from itch.io as a source |
| **URL Normalization** | Automatic handling of relative/absolute itch.io URLs |
| **HTML Conversion** | Converts HTML descriptions to formatted text |

### Localization
| Feature | Description |
|---------|-------------|
| **Multi-language** | Full support for English and Italian |
| **Auto-detection** | Automatically follows Playnite's language setting |
| **Complete Translation** | All UI strings, dialogs, settings, and messages are localized |

---

## Screenshots

> Use **Right-click on any game → Edit → Download Metadata → select itch.io as source** to fetch metadata.
> Or use **Library → Download Metadata** for bulk operations.

---

## Installation

### Quick Method (.pext file)

1. Download `ItchioMetadata-1.1.0.pext` from the [Releases](https://github.com/sassoanarchico/itchio-metadata-playnite/releases) page
2. In Playnite: **Menu → Extensions → Install extension from file**
3. Select the downloaded `.pext` file
4. **Restart Playnite**
5. The itch.io metadata provider will be available in the metadata download options

### Manual Installation (developers)

```powershell
# Clone the repository
git clone https://github.com/sassoanarchico/itchio-metadata-playnite.git
cd itchio-metadata-playnite

# Build
dotnet build -c Release
```

Or copy the compiled folder to `%AppData%\Playnite\Extensions\ItchioMetadata\`.

---

## Quick Start

### Downloading Metadata for a Single Game

1. **Right-click** on any game in your library
2. Select **Edit**
3. Click **Download Metadata**
4. Select **itch.io** as the metadata source
5. If the game has an itch.io link, it will be used directly
6. Otherwise, a search dialog will appear — select the correct game
7. Metadata will be downloaded and applied automatically

### Bulk Metadata Download

1. Go to **Library → Download Metadata**
2. Select the games you want to update
3. Choose **itch.io** as the metadata source
4. The extension will process all selected games

### Automatic Search

If a game doesn't have an itch.io link:
1. The extension will automatically search by game name
2. A selection dialog will show matching results with image previews
3. Select the correct game from the list
4. Metadata will be downloaded

---

## Settings

Accessible from **Menu → Extensions → Extension settings → itch.io Metadata Provider**.

| Setting | Default | Description |
|---------|---------|-------------|
| Prefer itch.io description | `Yes` | Replace existing description with itch.io description |
| Download screenshots as backgrounds | `No` | Download screenshots as background images |
| Auto-select first search result | `No` | Automatically use the first search result without showing dialog |
| Max search results | `20` | Maximum number of results to show in search (1–100) |

---

## Project Structure

```
itchio-metadata-playnite/
├── ItchioMetadataPlugin.cs       # Entry point: MetadataPlugin registration
├── ItchioMetadataProvider.cs     # OnDemandMetadataProvider: URL detection, search, field getters
├── ItchioMetadataSettings.cs     # Settings model with ISettings pattern
├── ItchioScraper.cs              # HTTP client + HtmlAgilityPack scraping
├── extension.yaml                # Playnite manifest (v1.1.0)
│
├── ItchioMetadataSettingsView.xaml      # Settings UI (checkboxes + text box)
├── ItchioMetadataSettingsView.xaml.cs   # Code-behind
│
├── Localization/
│   ├── en_US.xaml                # English strings
│   └── it_IT.xaml                # Italian strings
│
├── AssemblyInfo.cs               # Assembly metadata
├── ItchioMetadata.csproj         # .NET Framework 4.8 SDK-style project
├── ItchioMetadata.sln            # Visual Studio solution
├── CHANGELOG.txt                 # Version history
└── README.md                     # This file
```

---

## Development

### Prerequisites

- Visual Studio 2022+ or VS Code with C# extension
- .NET Framework 4.8 Developer Pack
- Playnite installed (for `Playnite.SDK.dll`)

### Building

```powershell
cd itchio-metadata-playnite
dotnet build -c Release
```

### NuGet Dependencies

| Package | Version | Usage |
|---------|---------|-------|
| HtmlAgilityPack | Latest | HTML parsing for web scraping |
| Playnite.SDK | 6.11.0+ | Playnite extension SDK |

### How Scraping Works

1. **URL Detection** — Checks if the game has an itch.io link in links, source, or GameId
2. **Search** — If no link found, searches itch.io by game name
3. **Page Scraping** — Downloads the game page HTML via `HttpClient`
4. **Data Extraction** — HtmlAgilityPack parses HTML to extract:
   - Title, description, developer, publisher
   - Cover image and screenshot URLs
   - Tags with genre inference
   - Release date (multi-format parsing)
   - Community rating (scaled to 0–100)
   - Additional links (Steam, social media, etc.)
5. **Metadata Application** — Fields are returned to Playnite's metadata pipeline

### Architecture

```
Playnite Host
  └─ ItchioMetadataPlugin (MetadataPlugin entry point)
       └─ ItchioMetadataProvider (OnDemandMetadataProvider, created per request)
            └─ ItchioScraper (HttpClient + HtmlAgilityPack)
                 ├─ SearchGames() → search page HTML → ItchioSearchResult[]
                 └─ GetGameMetadata() → game page HTML → ItchioGameMetadata
```

**Data flow**: Playnite → Plugin → Provider → Scraper → HTTP GET → HTML → HtmlAgilityPack → `ItchioGameMetadata` POCO → Playnite metadata fields.

---

## Roadmap

### High Priority
- [ ] **Fix `SelectGame()` re-search bug** — When the user types a new search term, the selected item is matched against the original results, not the new ones. Selection silently fails.
- [ ] **Dispose `ItchioScraper` properly** — `ItchioMetadataProvider` creates a scraper per request but never calls `Dispose()`, leaking `HttpClient`/socket handles
- [ ] **Use static `HttpClient`** — Currently a new `HttpClient` is created per scraper instance, causing socket exhaustion on bulk metadata downloads
- [ ] **Fix sync-over-async deadlock** — `GetHtmlContent()` uses `Task.Run().Wait()` which can deadlock on UI thread

### Medium Priority
- [ ] **Add rate limiting** — Bulk metadata downloads hammer itch.io with rapid requests, risking IP throttling
- [ ] **Add retry with backoff** — Transient network failures immediately fail the whole operation
- [ ] **Keep HTML formatting** — Description is stripped to plain text, but Playnite supports HTML
- [ ] **Honor `DownloadScreenshots` setting** — Setting exists but is never checked; screenshots are always included
- [ ] **Fix background download logic** — `PreferFirstSearchResult` setting is effectively meaningless during background downloads
- [ ] **Add metadata caching** — Repeated lookups for the same game re-scrape every time
- [ ] **Thread-safe `EnsureDataFetched()`** — Not thread-safe; concurrent access could trigger multiple fetches
- [ ] **Fix `.gitignore`** — `*.txt` pattern excludes `CHANGELOG.txt` from version control

### Future Ideas
- [ ] Cancellation token support for long HTTP calls
- [ ] Configurable HTTP timeout
- [ ] Tag-to-genre mapping configuration
- [ ] Local build script (`build.ps1`) for .pext packaging
- [ ] Unit tests with saved HTML fixtures
- [ ] Add extension icon (`icon.png`)
- [ ] More localization languages (Spanish, French, German)
- [ ] Smart date parsing (distinguish Release vs Updated dates)

---

## Known Issues

### Critical
| Issue | Description |
|-------|-------------|
| **Re-search selection broken** | When user types a new search term in the selection dialog, the selected item is matched against the *original* results, not the new search. Selection silently returns `null`. |
| **HttpClient/socket leak** | `ItchioScraper` implements `IDisposable` but is never disposed by `ItchioMetadataProvider`. Socket handles leak on every metadata request. |
| **Sync-over-async deadlock** | `GetHtmlContent()` uses `.Wait()` + `.Result` inside `Task.Run()`, which can deadlock on the UI thread when `AvailableFields` is accessed. |

### High
| Issue | Description |
|-------|-------------|
| **Scraping fragility** | All CSS selectors and XPath expressions are hardcoded. Any itch.io redesign silently breaks all metadata extraction. |
| **UI thread freeze** | `AvailableFields` getter calls `EnsureDataFetched()` which does network I/O, potentially blocking the UI thread. |
| **Description loses formatting** | HTML is stripped to plain text, losing bold, italic, links, headings, and images. |
| **`PreferFirstSearchResult` ignored** | During background downloads, the first result is always used regardless of this setting. |

### Medium
| Issue | Description |
|-------|-------------|
| **No rate limiting** | Bulk downloads hammer itch.io with rapid requests. |
| **`DownloadScreenshots` setting unused** | Setting exists in UI but is never checked in code. Screenshots are always returned. |
| **Genre mapping duplicates** | Tag "action-adventure" matches both "action" and "adventure" genres. |
| **Release date may return Update date** | Parser can match "Updated" rows instead of "Published" rows. |

---

## Changelog

See [CHANGELOG.txt](CHANGELOG.txt) for the complete list of changes.

### Latest versions

- **v1.1.0** — Complete localization support (English and Italian), all UI strings translated
- **v1.0.0** — Initial release with full metadata support, search functionality, and settings

---

## License

Distributed under the [MIT](LICENSE) license.

---

## Author

**Sassoanarchico** — [GitHub](https://github.com/sassoanarchico)

---

<div align="center">
<sub>Made with web scraping and HTML parsing for Playnite</sub>
</div>
