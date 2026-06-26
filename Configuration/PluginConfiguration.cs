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
    /// Comma-separated list of library names to process. Empty = all libraries.
    /// Example: "Movies, TV Shows"
    /// </summary>
    public string LibraryFilter { get; set; } = string.Empty;

    /// <summary>
    /// If true, also hide seasons that have no episodes.
    /// </summary>
    public bool HideEmptySeasons { get; set; } = true;

    /// <summary>
    /// If true, also hide collections/box-sets that have no items.
    /// </summary>
    public bool HideEmptyCollections { get; set; } = true;
}
