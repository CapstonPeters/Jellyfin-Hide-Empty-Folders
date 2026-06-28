using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.HideEmptyFolders;

/// <summary>
/// Listens for real-time item additions (file watcher, partial scans) and runs
/// empty folder cleanup on a debounced timer. This covers the gap that
/// ILibraryPostScanTask leaves — it only fires on full library scans, but
/// empty folders can also be added through real-time monitoring.
///
/// During a full scan, ILibraryManager.IsScanRunning is true and this handler
/// defers to ILibraryPostScanTask to avoid duplicate work.
/// </summary>
public sealed class RealTimeCleanupHost : IHostedService, IDisposable
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<RealTimeCleanupHost> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly object _timerLock = new();
    private Timer? _debounceTimer;
    private bool _disposed;

    /// <summary>
    /// How long to wait after the last folder item addition before running cleanup.
    /// Keeps resetting as items pour in during a bulk scan, then fires once after
    /// things settle. This prevents running cleanup hundreds of times during a scan.
    /// </summary>
    private static readonly TimeSpan DebounceInterval = TimeSpan.FromSeconds(10);

    public RealTimeCleanupHost(
        ILibraryManager libraryManager,
        ILogger<RealTimeCleanupHost> logger,
        IServiceProvider serviceProvider)
    {
        _libraryManager = libraryManager;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _libraryManager.ItemAdded += OnItemAdded;
        _logger.LogInformation("Real-time empty folder cleanup listener started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _libraryManager.ItemAdded -= OnItemAdded;
        DisposeTimer();
        _logger.LogInformation("Real-time empty folder cleanup listener stopped");
        return Task.CompletedTask;
    }

    private void OnItemAdded(object? sender, ItemChangeEventArgs e)
    {
        // We only care about folder-type items — a new empty show/season/folder
        if (e.Item is not Folder)
            return;

        // During a full scan, ILibraryPostScanTask will handle cleanup.
        // Running here too would be redundant (and potentially conflicting).
        if (_libraryManager.IsScanRunning)
        {
            _logger.LogDebug("Scan in progress — real-time handler deferring to post-scan task");
            return;
        }

        _logger.LogDebug("Folder added via real-time monitoring: {Name} ({Path})",
            e.Item.Name, e.Item.Path);

        // Debounce: keep resetting the timer as more items arrive.
        // Once the flood stops, fire once after DebounceInterval.
        lock (_timerLock)
        {
            if (_disposed) return;

            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(
                _ => RunCleanup(),
                null,
                DebounceInterval,
                Timeout.InfiniteTimeSpan);
        }
    }

    private void RunCleanup()
    {
        try
        {
            _logger.LogDebug("Running debounced real-time empty folder cleanup");

            var cleanupTask = (EmptyFolderCleanupTask?)_serviceProvider.GetService(typeof(EmptyFolderCleanupTask));
            if (cleanupTask == null)
            {
                _logger.LogWarning("Could not resolve EmptyFolderCleanupTask from DI — real-time cleanup skipped");
                return;
            }

            var progress = new Progress<double>();
            cleanupTask.Run(progress, CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during real-time empty folder cleanup");
        }
    }

    private void DisposeTimer()
    {
        lock (_timerLock)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _libraryManager.ItemAdded -= OnItemAdded;
        DisposeTimer();
    }
}
