using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.HideEmptyFolders.Configuration;

/// <summary>
/// Plugin configuration. Settings are persisted by Jellyfin automatically.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// If true, empty folders are hidden automatically after each library scan.
    /// </summary>
    public bool AutoCleanup { get; set; } = true;

    /// <summary>
    /// Collection folder IDs (library IDs) that the plugin is allowed to process.
    /// An empty list means ALL libraries are processed.
    /// </summary>
    public List<Guid> EnabledLibraryIds { get; set; } = new();

    /// <summary>
    /// If true, also hide seasons that have no episodes.
    /// </summary>
    public bool HideEmptySeasons { get; set; } = true;

    /// <summary>
    /// If true, also hide collections/box-sets that have no items.
    /// </summary>
    public bool HideEmptyCollections { get; set; } = true;
}
