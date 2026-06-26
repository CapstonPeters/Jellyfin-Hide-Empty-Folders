using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.HideEmptyFolders.ScheduledTasks;

/// <summary>
/// Scheduled task that triggers the empty folder cleanup on demand.
/// Appears in Dashboard → Scheduled Tasks and can be run manually by admins.
/// </summary>
public class HideEmptyFoldersTask : IScheduledTask
{
    private readonly EmptyFolderCleanupTask _cleanupTask;
    private readonly ILogger<HideEmptyFoldersTask> _logger;

    public HideEmptyFoldersTask(
        EmptyFolderCleanupTask cleanupTask,
        ILogger<HideEmptyFoldersTask> logger)
    {
        _cleanupTask = cleanupTask;
        _logger = logger;
    }

    public string Name => "Hide Empty Folders";
    public string Key => "Jellyfin.Plugin.HideEmptyFolders";
    public string Description => "Scans the library and removes folders that contain no media files. "
        + "Files on disk are NOT deleted — only the library entry is removed.";
    public string Category => "Library";

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        // No default schedule — user triggers manually or relies on the ILibraryPostScanTask
        return Array.Empty<TaskTriggerInfo>();
    }

    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Manual Hide Empty Folders task started");
        await _cleanupTask.Run(progress, cancellationToken);
        _logger.LogInformation("Manual Hide Empty Folders task completed");
    }
}
