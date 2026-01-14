# itch.io Metadata Provider

Playnite plugin to fetch game metadata from itch.io via web scraping.

## What it does

Grabs metadata from itch.io game pages - title, description, cover art, screenshots, tags, developer info, release date, ratings. Works by scraping the HTML since itch.io doesn't have a public metadata API.

## Installation

Build with `dotnet build -c Release` and copy the output to `%AppData%\Playnite\Extensions\`. Or use the install script.

You can also pack it as .pext (just zip the dll + extension.yaml + icon.png).

## Building

Requires .NET Framework 4.6.2.

```
dotnet build -c Release
```

## Usage

Right-click any game → Edit → Download Metadata → select itch.io as source.

Or bulk download: Library → Download Metadata.

If the game already has an itch.io link, it'll use that directly. Otherwise it searches by name.

## Settings

- Prefer itch.io description over existing
- Download screenshots as backgrounds  
- Auto-select first search result
- Max results limit

## Known issues

- Scraping can break if itch.io changes their page layout
- Not all games have ratings
- Some indie devs don't fill in all the metadata fields

## License

MIT
