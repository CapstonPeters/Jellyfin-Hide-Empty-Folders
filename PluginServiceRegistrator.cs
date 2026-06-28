using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.HideEmptyFolders;

/// <summary>
/// Registers plugin services with Jellyfin's DI container.
/// This is the critical missing piece: without it, EmptyFolderCleanupTask
/// is never added to DI, so Jellyfin never discovers it as an ILibraryPostScanTask
/// and the "Run automatically after each library scan" feature silently does nothing.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<ILibraryPostScanTask, EmptyFolderCleanupTask>();
    }
}
