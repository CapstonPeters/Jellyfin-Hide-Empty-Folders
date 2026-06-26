# Jellyfin Hide Empty Folders

Hides empty folders from Jellyfin library views. Folders that contain no media files are removed from the library database — **your files on disk are never touched**.

## The Problem

Jellyfin shows folders in your library even when they contain no video/audio files. This clutters your library with empty seasons, shows you haven't added yet, or folders that only contain metadata/subtitle files.

## How It Works

1. After every library scan, the plugin checks all folder-type items (Series, Seasons, BoxSets, Collections)
2. Folders with **no media descendants** (Episodes, Movies, Audio files, etc.) are identified
3. Those folders are removed from the Jellyfin library database
4. **Files on disk are NEVER deleted** — if you add media later, the folder reappears on the next scan

## Features

- 🔄 **Automatic**: runs after every library scan — no manual intervention needed
- 🛡️ **Safe**: never touches your files, only removes library entries
- ⚙️ **Configurable**: per-library filtering, toggle empty season/collection cleanup
- 🖐️ **Manual trigger**: run anytime from Dashboard → Scheduled Tasks
- 📊 **Logged**: all removals logged with folder names and paths

## Installation

1. Download the latest `Jellyfin.Plugin.HideEmptyFolders.dll` from [Releases](https://github.com/CapstonPeters/Jellyfin-Hide-Empty-Folders/releases)
2. Place it in your Jellyfin plugins directory:
   - **Linux**: `/var/lib/jellyfin/plugins/HideEmptyFolders/`
   - **Windows**: `%AppData%\jellyfin\plugins\HideEmptyFolders\`
   - **Docker**: `{config}/plugins/HideEmptyFolders/`
3. Restart Jellyfin

## Configuration

Go to **Dashboard → Plugins → Hide Empty Folders → Settings**:

| Setting | Default | Description |
|---------|---------|-------------|
| Auto Cleanup | ✅ On | Run automatically after each library scan |
| Hide Empty Seasons | ✅ On | Also hide seasons with no episodes |
| Hide Empty Collections | ✅ On | Also hide empty box-sets/collections |
| Library Filter | (all) | Comma-separated list of library names to process |

## Manual Trigger

**Dashboard → Scheduled Tasks → Hide Empty Folders** — click the play button to run on demand.

## Building from Source

```bash
dotnet restore
dotnet build --configuration Release
```

Output: `bin/Release/net9.0/Jellyfin.Plugin.HideEmptyFolders.dll`

## Requirements

- Jellyfin 10.9.0+ (targets .NET 9.0)
- Plugin targets Jellyfin.Controller/Jellyfin.Model 10.11.3
