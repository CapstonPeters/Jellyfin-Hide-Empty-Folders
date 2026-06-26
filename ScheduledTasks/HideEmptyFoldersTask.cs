using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.HideEmptyFolders.ScheduledTasks;

/// <summary>
/// Scheduled task that triggers the empty folder cleanup on demand.
/// Appears in Dashboard → Scheduled Tasks and can be run manually.
///
/// Constructs EmptyFolderCleanupTask manually to avoid DI resolution issues:
/// Jellyfin registers it only as ILibraryPostScanTask, not as the concrete type.
/// </summary>
public class HideEmptyFoldersTask : IScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<HideEmptyFoldersTask> _logger;

    public HideEmptyFoldersTask(
        ILibraryManager libraryManager,
        ILoggerFactory loggerFactory,
        ILogger<HideEmptyFoldersTask> logger)
    {
        _libraryManager = libraryManager;
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    public string Name => "Hide Empty Folders";
    public string Key => "HideEmptyFolders";
    public string Description => "Scans the library and removes folders that contain no media files. "
        + "Files on disk are NOT deleted — only the library entry is removed.";
    public string Category => "Library";

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return Array.Empty<TaskTriggerInfo>();
    }

    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Manual Hide Empty Folders task started");

        var cleanupLogger = _loggerFactory.CreateLogger<EmptyFolderCleanupTask>();
        var cleanupTask = new EmptyFolderCleanupTask(_libraryManager, cleanupLogger);
        return cleanupTask.Run(progress, cancellationToken);
    }
}
