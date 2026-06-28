using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.HideEmptyFolders;

/// <summary>
/// Listens for real-time library changes (ItemAdded, ItemRemoved) and runs
/// empty folder cleanup on a debounced timer. This covers the gap that
/// ILibraryPostScanTask leaves — it only fires on full library scans, but
/// items can be added or removed through real-time monitoring, partial scans,
/// or direct API calls without triggering a full scan.
///
/// The debounce (10s) coalesces rapid-fire events during bulk operations
/// into a single cleanup run. A re-entry guard prevents cleanup from running
/// concurrently with itself (e.g. when DeleteItem inside cleanup fires
/// ItemRemoved which re-schedules another cleanup).
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
    /// Prevents recursive cleanup: when EmptyFolderCleanupTask deletes items,
    /// those deletions fire ItemRemoved which would re-schedule cleanup.
    /// This flag ensures only one cleanup runs at a time.
    /// 0 = idle, 1 = running.
    /// </summary>
    private int _cleanupInProgress;

    /// <summary>
    /// How long to wait after the last library change before running cleanup.
    /// Keeps resetting as events pour in, then fires once after things settle.
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
        _libraryManager.ItemAdded += OnLibraryChanged;
        _libraryManager.ItemRemoved += OnLibraryChanged;
        _logger.LogInformation("Real-time empty folder cleanup listener started (ItemAdded + ItemRemoved)");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _libraryManager.ItemAdded -= OnLibraryChanged;
        _libraryManager.ItemRemoved -= OnLibraryChanged;
        DisposeTimer();
        _logger.LogInformation("Real-time empty folder cleanup listener stopped");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called on every item addition or removal. Debounces to avoid running
    /// cleanup hundreds of times during bulk operations. If cleanup is already
    /// in progress, still schedules a follow-up for after it finishes.
    /// </summary>
    private void OnLibraryChanged(object? sender, ItemChangeEventArgs e)
    {
        // Debounce: keep resetting the timer as more changes arrive.
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
        // Prevent recursive cleanup: DeleteItem inside EmptyFolderCleanupTask
        // fires ItemRemoved, which re-schedules this callback. If we're already
        // running, skip this cycle — the re-scheduled timer will fire after
        // we finish, catching any newly-emptied folders.
        if (Interlocked.CompareExchange(ref _cleanupInProgress, 1, 0) == 1)
        {
            _logger.LogDebug("Cleanup already in progress — skipping re-entrant cycle");
            return;
        }

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
        finally
        {
            Interlocked.Exchange(ref _cleanupInProgress, 0);
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

        _libraryManager.ItemAdded -= OnLibraryChanged;
        _libraryManager.ItemRemoved -= OnLibraryChanged;
        DisposeTimer();
    }
}
