using Playnite.SDK;
using Playnite.SDK.Data;
using System.Collections.Generic;

namespace ItchioMetadata
{
    public class ItchioMetadataSettings : ObservableObject, ISettings
    {
        private readonly ItchioMetadataPlugin plugin;

        private bool preferItchioDescription = true;
        private bool downloadScreenshots = true;
        private int maxSearchResults = 20;
        private bool preferFirstSearchResult = false;

        public bool PreferItchioDescription
        {
            get => preferItchioDescription;
            set => SetValue(ref preferItchioDescription, value);
        }

        public bool DownloadScreenshots
        {
            get => downloadScreenshots;
            set => SetValue(ref downloadScreenshots, value);
        }

        public int MaxSearchResults
        {
            get => maxSearchResults;
            set => SetValue(ref maxSearchResults, value);
        }

        public bool PreferFirstSearchResult
        {
            get => preferFirstSearchResult;
            set => SetValue(ref preferFirstSearchResult, value);
        }

        // Previous settings for cancel functionality
        private ItchioMetadataSettings previousSettings;

        public ItchioMetadataSettings()
        {
        }

        public ItchioMetadataSettings(ItchioMetadataPlugin plugin)
        {
            this.plugin = plugin;

            var savedSettings = plugin.LoadPluginSettings<ItchioMetadataSettings>();
            if (savedSettings != null)
            {
                PreferItchioDescription = savedSettings.PreferItchioDescription;
                DownloadScreenshots = savedSettings.DownloadScreenshots;
                MaxSearchResults = savedSettings.MaxSearchResults;
                PreferFirstSearchResult = savedSettings.PreferFirstSearchResult;
            }
        }

        public void BeginEdit()
        {
            previousSettings = new ItchioMetadataSettings
            {
                PreferItchioDescription = this.PreferItchioDescription,
                DownloadScreenshots = this.DownloadScreenshots,
                MaxSearchResults = this.MaxSearchResults,
                PreferFirstSearchResult = this.PreferFirstSearchResult
            };
        }

        public void CancelEdit()
        {
            if (previousSettings != null)
            {
                PreferItchioDescription = previousSettings.PreferItchioDescription;
                DownloadScreenshots = previousSettings.DownloadScreenshots;
                MaxSearchResults = previousSettings.MaxSearchResults;
                PreferFirstSearchResult = previousSettings.PreferFirstSearchResult;
            }
        }

        public void EndEdit()
        {
            plugin.SavePluginSettings(this);
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();

            if (MaxSearchResults < 1 || MaxSearchResults > 100)
            {
                errors.Add("Max search results must be between 1 and 100");
            }

            return errors.Count == 0;
        }
    }
}
