using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace ItchioMetadata
{
    public class ItchioMetadataPlugin : MetadataPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private ItchioMetadataSettings settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("c7d4b7e9-8f1a-4b2c-9e3d-5f6a7b8c9d0e");

        public override string Name => "itch.io Metadata";

        public override List<MetadataField> SupportedFields { get; } = new List<MetadataField>
        {
            MetadataField.Name,
            MetadataField.Description,
            MetadataField.Developers,
            MetadataField.Publishers,
            MetadataField.Genres,
            MetadataField.Tags,
            MetadataField.ReleaseDate,
            MetadataField.CoverImage,
            MetadataField.BackgroundImage,
            MetadataField.Links,
            MetadataField.CommunityScore
        };

        public ItchioMetadataPlugin(IPlayniteAPI api) : base(api)
        {
            settings = new ItchioMetadataSettings(this);
            Properties = new MetadataPluginProperties
            {
                HasSettings = true
            };
        }

        public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
        {
            return new ItchioMetadataProvider(options, this);
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new ItchioMetadataSettingsView();
        }

        public IPlayniteAPI GetPlayniteAPI()
        {
            return PlayniteApi;
        }

        public ItchioMetadataSettings GetSettings()
        {
            return settings;
        }
    }
}
