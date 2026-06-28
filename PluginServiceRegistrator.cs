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
        // Register EmptyFolderCleanupTask as a singleton so it can be resolved
        // both by concrete type (RealTimeCleanupHost) and by interface
        // (Jellyfin's ILibraryPostScanTask discovery).
        serviceCollection.AddSingleton<EmptyFolderCleanupTask>();
        serviceCollection.AddSingleton<ILibraryPostScanTask>(
            sp => sp.GetRequiredService<EmptyFolderCleanupTask>());

        // Handles real-time folder additions/deletions (file watcher, partial scans)
        // that bypass ILibraryPostScanTask. Debounces cleanup to avoid redundant work.
        serviceCollection.AddSingleton<IHostedService, RealTimeCleanupHost>();
    }
}
