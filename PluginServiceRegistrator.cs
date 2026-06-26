using MediaBrowser.Common.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.HideEmptyFolders;

/// <summary>
/// Registers plugin services in the Jellyfin DI container.
/// Without this, EmptyFolderCleanupTask is only registered as ILibraryPostScanTask,
/// and HideEmptyFoldersTask (which injects the concrete type) fails to resolve.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<EmptyFolderCleanupTask>();
    }
}
