using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ItchioMetadata
{
    public class ItchioMetadataProvider : OnDemandMetadataProvider
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private readonly MetadataRequestOptions options;
        private readonly ItchioMetadataPlugin plugin;
        private readonly ItchioScraper scraper;

        private ItchioGameMetadata gameMetadata;
        private bool dataFetched = false;

        public override List<MetadataField> AvailableFields
        {
            get
            {
                EnsureDataFetched();
                return GetAvailableFields();
            }
        }

        public ItchioMetadataProvider(MetadataRequestOptions options, ItchioMetadataPlugin plugin)
        {
            this.options = options;
            this.plugin = plugin;
            this.scraper = new ItchioScraper();
        }

        private void EnsureDataFetched()
        {
            if (dataFetched)
                return;

            try
            {
                string gameUrl = GetItchioUrl();
                if (!string.IsNullOrEmpty(gameUrl))
                {
                    gameMetadata = scraper.GetGameMetadata(gameUrl);
                }
                else
                {
                    string gameName = options.GameData.Name;
                    if (!string.IsNullOrEmpty(gameName))
                    {
                        var settings = plugin.GetSettings();
                        int maxResults = settings?.MaxSearchResults ?? 20;
                        var searchResults = scraper.SearchGames(gameName, maxResults);
                        if (searchResults.Any())
                        {
                            bool useFirstResult = options.IsBackgroundDownload && (settings?.PreferFirstSearchResult ?? false);
                            if (searchResults.Count == 1 || useFirstResult || !options.IsBackgroundDownload)
                            {
                                if (useFirstResult || searchResults.Count == 1)
                                {
                                    gameMetadata = scraper.GetGameMetadata(searchResults.First().Url);
                                }
                                else
                                {
                                    var selectedGame = SelectGame(searchResults);
                                    if (selectedGame != null)
                                    {
                                        gameMetadata = scraper.GetGameMetadata(selectedGame.Url);
                                    }
                                }
                            }
                            else
                            {
                                gameMetadata = scraper.GetGameMetadata(searchResults.First().Url);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to fetch itch.io metadata");
            }

            dataFetched = true;
        }

        private string GetItchioUrl()
        {
            if (options.GameData.Links != null)
            {
                var itchioLink = options.GameData.Links.FirstOrDefault(l => 
                    l.Url?.Contains("itch.io") == true);
                if (itchioLink != null)
                {
                    string url = itchioLink.Url;
                    // Normalize URL if needed
                    if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                    {
                        if (url.StartsWith("/"))
                        {
                            url = "https://itch.io" + url;
                        }
                        else
                        {
                            url = "https://itch.io/" + url;
                        }
                    }
                    return url;
                }
            }

            if (options.GameData.GameId?.Contains("itch.io") == true)
            {
                string url = options.GameData.GameId;
                // Normalize URL if needed
                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                {
                    if (url.StartsWith("/"))
                    {
                        url = "https://itch.io" + url;
                    }
                    else
                    {
                        url = "https://itch.io/" + url;
                    }
                }
                return url;
            }

            if (options.GameData.Source?.Name?.ToLower() == "itch.io" ||
                options.GameData.Source?.Name?.ToLower() == "itch")
            {
                if (!string.IsNullOrEmpty(options.GameData.GameId))
                {
                    string url = options.GameData.GameId;
                    if (url.StartsWith("http"))
                    {
                        return url;
                    }
                    // Try to construct URL from GameId
                    if (url.StartsWith("/"))
                    {
                        return "https://itch.io" + url;
                    }
                    else
                    {
                        return "https://itch.io/" + url;
                    }
                }
            }

            return null;
        }

        private ItchioSearchResult SelectGame(List<ItchioSearchResult> searchResults)
        {
            if (options.IsBackgroundDownload)
            {
                return searchResults.FirstOrDefault();
            }

            var selectedItem = plugin.GetPlayniteAPI().Dialogs.ChooseItemWithSearch(
                searchResults.Select(r => new GenericItemOption(r.Title, r.Description)).ToList(),
                (searchTerm) =>
                {
                    var settings = plugin.GetSettings();
                    int maxResults = settings?.MaxSearchResults ?? 20;
                    var newResults = scraper.SearchGames(searchTerm, maxResults);
                    return newResults.Select(r => new GenericItemOption(r.Title, r.Description)).ToList();
                },
                options.GameData.Name,
                "Select itch.io game");

            if (selectedItem != null)
            {
                var index = searchResults.FindIndex(r => r.Title == selectedItem.Name);
                if (index >= 0)
                {
                    return searchResults[index];
                }
            }

            return null;
        }

        private List<MetadataField> GetAvailableFields()
        {
            var fields = new List<MetadataField>();

            if (gameMetadata == null)
                return fields;

            if (!string.IsNullOrEmpty(gameMetadata.Name))
                fields.Add(MetadataField.Name);
            if (!string.IsNullOrEmpty(gameMetadata.Description))
                fields.Add(MetadataField.Description);
            if (gameMetadata.Developers?.Any() == true)
                fields.Add(MetadataField.Developers);
            if (gameMetadata.Publishers?.Any() == true)
                fields.Add(MetadataField.Publishers);
            if (gameMetadata.Genres?.Any() == true)
                fields.Add(MetadataField.Genres);
            if (gameMetadata.Tags?.Any() == true)
                fields.Add(MetadataField.Tags);
            if (gameMetadata.ReleaseDate.HasValue)
                fields.Add(MetadataField.ReleaseDate);
            if (!string.IsNullOrEmpty(gameMetadata.CoverImageUrl))
                fields.Add(MetadataField.CoverImage);
            if (gameMetadata.Screenshots?.Any() == true)
                fields.Add(MetadataField.BackgroundImage);
            if (gameMetadata.Links?.Any() == true)
                fields.Add(MetadataField.Links);
            if (gameMetadata.CommunityScore.HasValue)
                fields.Add(MetadataField.CommunityScore);

            return fields;
        }

        public override string GetName(GetMetadataFieldArgs args)
        {
            EnsureDataFetched();
            return gameMetadata?.Name;
        }

        public override string GetDescription(GetMetadataFieldArgs args)
        {
            EnsureDataFetched();
            var settings = plugin.GetSettings();
            // Only use itch.io description if prefer setting is enabled or if no existing description
            if (gameMetadata != null && !string.IsNullOrEmpty(gameMetadata.Description))
            {
                if (settings.PreferItchioDescription || string.IsNullOrEmpty(options.GameData.Description))
                {
                    return gameMetadata.Description;
                }
            }
            return null;
        }

        public override IEnumerable<MetadataProperty> GetDevelopers(GetMetadataFieldArgs args)
        {
            EnsureDataFetched();
            return gameMetadata?.Developers?.Select(d => new MetadataNameProperty(d));
        }

        public override IEnumerable<MetadataProperty> GetPublishers(GetMetadataFieldArgs args)
        {
            EnsureDataFetched();
            return gameMetadata?.Publishers?.Select(p => new MetadataNameProperty(p));
        }

        public override IEnumerable<MetadataProperty> GetGenres(GetMetadataFieldArgs args)
        {
            EnsureDataFetched();
            return gameMetadata?.Genres?.Select(g => new MetadataNameProperty(g));
        }

        public override IEnumerable<MetadataProperty> GetTags(GetMetadataFieldArgs args)
        {
            EnsureDataFetched();
            return gameMetadata?.Tags?.Select(t => new MetadataNameProperty(t));
        }

        public override ReleaseDate? GetReleaseDate(GetMetadataFieldArgs args)
        {
            EnsureDataFetched();
            if (gameMetadata?.ReleaseDate.HasValue == true)
            {
                return new ReleaseDate(gameMetadata.ReleaseDate.Value);
            }
            return null;
        }

        public override MetadataFile GetCoverImage(GetMetadataFieldArgs args)
        {
            EnsureDataFetched();
            if (!string.IsNullOrEmpty(gameMetadata?.CoverImageUrl))
            {
                return new MetadataFile(gameMetadata.CoverImageUrl);
            }
            return null;
        }

        public override MetadataFile GetBackgroundImage(GetMetadataFieldArgs args)
        {
            EnsureDataFetched();
            if (gameMetadata?.Screenshots?.Any() == true)
            {
                if (options.IsBackgroundDownload)
                {
                    return new MetadataFile(gameMetadata.Screenshots.First());
                }

                var imageOptions = gameMetadata.Screenshots
                    .Select(s => new ImageFileOption(s))
                    .ToList<ImageFileOption>();

                var selected = plugin.GetPlayniteAPI().Dialogs.ChooseImageFile(
                    imageOptions, "Select background image");

                if (selected != null)
                {
                    return new MetadataFile(selected.Path);
                }
            }
            return null;
        }

        public override IEnumerable<Link> GetLinks(GetMetadataFieldArgs args)
        {
            EnsureDataFetched();
            return gameMetadata?.Links;
        }

        public override int? GetCommunityScore(GetMetadataFieldArgs args)
        {
            EnsureDataFetched();
            return gameMetadata?.CommunityScore;
        }
    }
}
