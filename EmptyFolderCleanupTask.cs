using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using Jellyfin.Plugin.HideEmptyFolders.Configuration;

namespace Jellyfin.Plugin.HideEmptyFolders;

/// <summary>
/// Runs after every library scan. Finds folders with no media-containing descendants
/// and removes them from the library database (without touching files on disk).
/// </summary>
public class EmptyFolderCleanupTask : ILibraryPostScanTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<EmptyFolderCleanupTask> _logger;

    /// <summary>
    /// Folder item kinds that should be checked for emptiness.
    /// </summary>
    private static readonly BaseItemKind[] FolderKinds =
    {
        BaseItemKind.Series,
        BaseItemKind.Season,
        BaseItemKind.BoxSet,
        BaseItemKind.Folder,
    };

    /// <summary>
    /// Leaf item kinds that count as "media content".
    /// A folder is NOT empty if it contains (directly or indirectly) any of these.
    /// </summary>
    private static readonly BaseItemKind[] MediaLeafKinds =
    {
        BaseItemKind.Episode,
        BaseItemKind.Movie,
        BaseItemKind.Audio,
        BaseItemKind.MusicVideo,
        BaseItemKind.Video,
        BaseItemKind.Trailer,
    };

    public EmptyFolderCleanupTask(
        ILibraryManager libraryManager,
        ILogger<EmptyFolderCleanupTask> logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
    }

    public Task Run(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration;
        if (config is { AutoCleanup: false })
        {
            _logger.LogDebug("Auto-cleanup disabled in plugin config; skipping");
            return Task.CompletedTask;
        }

        progress.Report(0);

        try
        {
            // ── Resolve which libraries to process ──────────────────
            // When EnabledLibraryIds is empty (fresh install or user checked all),
            // default to only TV Show (tvshows) libraries to avoid accidentally
            // cleaning up Movies, Music, or other media libraries.
            HashSet<Guid> effectiveLibraryIds;
            if (config?.EnabledLibraryIds is { Count: > 0 })
            {
                effectiveLibraryIds = new HashSet<Guid>(config.EnabledLibraryIds);
                _logger.LogDebug("Using {Count} explicitly configured library IDs", effectiveLibraryIds.Count);
            }
            else
            {
                effectiveLibraryIds = GetTvShowLibraryIds();
                _logger.LogInformation(
                    "No libraries explicitly configured — defaulting to {Count} TV Show library(s)",
                    effectiveLibraryIds.Count);
            }

            // ── Step 1: Get all folder-type items ──────────────────
            _logger.LogInformation("Scanning for empty folders...");

            var folderQuery = new InternalItemsQuery
            {
                IncludeItemTypes = FolderKinds,
                IsVirtualItem = false,
                Recursive = true,
            };

            var folders = _libraryManager.GetItemList(folderQuery);
            _logger.LogDebug("Found {Count} folder items to check", folders.Count);
            progress.Report(20);

            // ── Step 2: Find which folders have media descendants ──
            var foldersWithContent = new HashSet<Guid>();

            // Query ALL leaf media items in the library
            var mediaQuery = new InternalItemsQuery
            {
                IncludeItemTypes = MediaLeafKinds,
                IsVirtualItem = false,
                Recursive = true,
            };

            var mediaItems = _libraryManager.GetItemList(mediaQuery);
            _logger.LogDebug("Found {Count} media items in library", mediaItems.Count);
            progress.Report(50);

            // Mark every ancestor of each media item as "has content"
            foreach (var item in mediaItems)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var parentId = item.ParentId;
                while (parentId != Guid.Empty)
                {
                    foldersWithContent.Add(parentId);

                    // Walk up to the parent
                    var parent = _libraryManager.GetItemById(parentId);
                    if (parent == null) break;
                    parentId = parent.ParentId;

                    // Safety: don't walk up past collection folders
                    if (parent is CollectionFolder) break;
                }
            }
            progress.Report(70);

            _logger.LogDebug("Found {Count} folders with media content", foldersWithContent.Count);

            // ── Step 3: Delete folders not in the "has content" set ──
            var emptyFolders = new List<BaseItem>();

            foreach (var folder in folders)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Skip if it has media descendants
                if (foldersWithContent.Contains(folder.Id))
                    continue;

                // NEVER delete library roots (CollectionFolders) — they are the top-level
                // containers like "Movies" and "TV Shows". Even if they appear empty,
                // removing them deletes the entire library from Jellyfin.
                if (folder is CollectionFolder)
                    continue;

                // Respect library selection — only process folders
                // belonging to the effective library set.
                var topParent = FindTopLibraryFolder(folder);
                if (topParent != null && !effectiveLibraryIds.Contains(topParent.Id))
                    continue;

                // Respect per-type settings
                var kind = folder.GetBaseItemKind();
                if (kind == BaseItemKind.Season && config is { HideEmptySeasons: false })
                    continue;
                if (kind == BaseItemKind.BoxSet && config is { HideEmptyCollections: false })
                    continue;

                emptyFolders.Add(folder);
            }

            _logger.LogInformation("Removing {Count} empty folders from library", emptyFolders.Count);
            progress.Report(85);

            var deleteOptions = new DeleteOptions
            {
                DeleteFileLocation = false,         // NEVER delete from disk!
                DeleteFromExternalProvider = false,
            };

            int removed = 0;
            foreach (var folder in emptyFolders)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    _logger.LogDebug("Removing empty folder: {Name} ({Path})",
                        folder.Name, folder.Path);
                    _libraryManager.DeleteItem(folder, deleteOptions);
                    removed++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to remove folder: {Name}", folder.Name);
                }
            }

            progress.Report(100);
            _logger.LogInformation("Hide Empty Folders complete: {Removed} folders removed", removed);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Empty folder cleanup was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during empty folder cleanup");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns the IDs of all CollectionFolders whose CollectionType is "tvshows".
    /// Used as the default library filter when no libraries are explicitly configured.
    /// </summary>
    private HashSet<Guid> GetTvShowLibraryIds()
    {
        var tvIds = new HashSet<Guid>();

        var query = new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.CollectionFolder },
            IsVirtualItem = false,
        };

        var collectionFolders = _libraryManager.GetItemList(query);
        foreach (var folder in collectionFolders)
        {
            if (folder is CollectionFolder cf
                && string.Equals(cf.CollectionType, "tvshows", StringComparison.OrdinalIgnoreCase))
            {
                tvIds.Add(cf.Id);
                _logger.LogDebug("Default-enabled TV Show library: {Name} ({Id})", cf.Name, cf.Id);
            }
        }

        return tvIds;
    }

    /// <summary>
    /// Walks up the parent chain to find the top-level CollectionFolder.
    /// </summary>
    private BaseItem? FindTopLibraryFolder(BaseItem item)
    {
        var current = item;
        while (current != null)
        {
            if (current.GetBaseItemKind() == BaseItemKind.CollectionFolder)
                return current;

            if (current.ParentId == Guid.Empty)
                break;

            current = _libraryManager.GetItemById(current.ParentId);
        }

        return null;
    }
}
