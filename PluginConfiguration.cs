using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.RandomCollectionsHome
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public int RandomCount { get; set; } = 3;
        
        // Video mode selections (all enabled by default)
        public bool UsePortrait { get; set; } = true;
        public bool UseSquare { get; set; } = true;
        public bool UseLandscape { get; set; } = true;
        
        // Collection settings
        public int CollectionLimit { get; set; } = 20;
        public int CollectionUpdateInterval { get; set; } = 1440; // 24 hours in minutes
        
        // Developer mode settings
        public bool DeveloperMode { get; set; } = false;
        public int DebugRefreshInterval { get; set; } = 60; // Default 60 seconds
    }
}


