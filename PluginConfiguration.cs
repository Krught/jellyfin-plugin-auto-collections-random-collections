using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.RandomCollectionsHome
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public int RandomCount { get; set; } = 3;
    }
}


