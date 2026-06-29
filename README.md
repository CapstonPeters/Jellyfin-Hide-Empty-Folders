<p align="center">
  <img src="logo.png" alt="Hide Empty Folders" width="200">
</p>

# Hide Empty Folders

A Jellyfin plugin that keeps your library clean. Empty folders — seasons with no episodes, box sets with no items, vacant series folders — are removed from the library view automatically. Nothing is ever deleted from disk.

[![Listed on JellyWatch Hub](https://jellywatch.app/hub/hide-empty-folders/badge.svg)](https://jellywatch.app/hub/hide-empty-folders)

---

## Installation

### Repository (recommended)

Add this URL to Jellyfin under **Dashboard → Plugins → Repositories → Add**:

```
https://raw.githubusercontent.com/CapstonPeters/Jellyfin-Hide-Empty-Folders/main/manifest.json
```

Then go to **Catalog**, find *Hide Empty Folders*, and click Install. Restart Jellyfin.

### Manual

1. Download the latest ZIP from [Releases](https://github.com/CapstonPeters/Jellyfin-Hide-Empty-Folders/releases)
2. Extract into your Jellyfin plugin folder — the resulting directory should look like:
   ```
   plugins/Hide Empty Folders/
   ├── Jellyfin.Plugin.HideEmptyFolders.dll
   ├── thumb.png
   └── meta.json
   ```
3. Restart Jellyfin

---

## Configuration

Open **Dashboard → Plugins → Hide Empty Folders → Settings**.

| Setting | What it does |
|---|---|
| **Libraries** | Checkboxes for each of your libraries. Only checked libraries are processed. By default, only TV Show libraries are checked — enable others explicitly if you want Movies, Music, etc. cleaned too. |
| **Run automatically** | Runs cleanup after every library scan. Turn off if you only want to trigger it manually. |
| **Hide empty seasons** | TV seasons with zero episodes are removed from view. |
| **Hide empty collections** | Box sets and collections with no items are removed from view. |

### Manual trigger

Go to **Dashboard → Scheduled Tasks → Hide Empty Folders** and click the play button.

---

## How it works

After a library scan completes (or you trigger it manually), the plugin:

1. Finds all media items across your selected libraries
2. Walks up the folder tree marking every ancestor as "has content"
3. Removes database entries for folders that have no media anywhere under them

The plugin uses `DeleteFileLocation = false` — folder entries are removed from Jellyfin's database only. Your files stay exactly where they are.

Collection folders (the top-level "Movies", "TV Shows" containers) are never touched.

**Pseudo season folders are preserved.** Jellyfin can create virtual season entries for TV libraries where episodes live in a flat folder (no real season subfolders). These pseudo seasons are left intact — only seasons whose parent series genuinely has zero media content are removed.

---

## What to expect

**Jellyfin is the party that adds empty folders.** The plugin only removes them from the library database — it never touches files on disk.

- **If a show is added to an empty folder:** Jellyfin's real-time monitoring (or scheduled scan) picks up the new file, re-adds the previously-removed empty folder structure to the library, then the plugin removes it again within ~10 seconds. The folder may briefly reappear and vanish — this is normal.
- **If you run a manual library scan:** The plugin runs immediately after the scan completes and cleans up any empty folders.
- **If you trigger the scheduled task** (Dashboard → Scheduled Tasks → Hide Empty Folders): It runs the same cleanup on demand.
- **Files on disk are never deleted.** The plugin uses `DeleteFileLocation = false` and skips CollectionFolder (library root) items entirely.

---

## Building

Requires .NET 9.0 SDK.

```bash
dotnet restore
dotnet build --configuration Release
```

The output DLL goes in `bin/Release/net9.0/`. Bundle it with `thumb.png` and `meta.json` for distribution.

---
