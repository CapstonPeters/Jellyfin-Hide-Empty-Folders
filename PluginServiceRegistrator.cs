using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Jellyfin.Plugin.HideEmptyFolders;

/// <summary>
/// Registers plugin services with Jellyfin's DI container.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        // Critical: registers EmptyFolderCleanupTask as ILibraryPostScanTask so it
        // runs automatically after every full library scan.
        serviceCollection.AddSingleton<ILibraryPostScanTask, EmptyFolderCleanupTask>();

        // Handles real-time folder additions (file watcher, partial scans) that
        // bypass ILibraryPostScanTask. Debounces cleanup to avoid redundant work.
        serviceCollection.AddSingleton<IHostedService, RealTimeCleanupHost>();
    }
}
