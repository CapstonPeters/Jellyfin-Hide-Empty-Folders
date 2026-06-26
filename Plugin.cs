using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.HideEmptyFolders;

/// <summary>
/// Hides folders that contain no media files from the Jellyfin library.
/// Folders are removed from the library database but NOT deleted from disk.
/// A background task runs after every library scan and can also be triggered manually.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public override string Name => "Hide Empty Folders";
    public override Guid Id => Guid.Parse("a8f3d2e1-5b7c-4a9e-8d1f-2c3b4a5e6f7d");

    public Plugin(
        IApplicationPaths applicationPaths,
        IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    public static Plugin? Instance { get; private set; }

    /// <summary>
    /// Returns the plugin configuration page to show in the Jellyfin dashboard.
    /// </summary>
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configPage.html",
            },
        };
    }
}
