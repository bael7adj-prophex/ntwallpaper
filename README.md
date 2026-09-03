# Pixabay Wallpaper Manager

A production-quality Windows desktop application that discovers, downloads, caches, and
automatically applies wallpapers from [Pixabay](https://pixabay.com) across monitors,
the lock screen, and virtual desktops.

Target: **Windows 10 x64 / Windows 11 x64**, .NET 10, WPF, MVVM, DI.

---

## Features

- **Pixabay integration** with all documented search parameters (q, lang, image_type,
  orientation, category, min_width, min_height, colors, editors_choice, safesearch,
  order, page, per_page)
- **Multiple-monitor** wallpaper assignment via the documented
  `IDesktopWallpaper` COM API (`Fill`, `Fit`, `Stretch`, `Tile`, `Center`, `Span`)
- **Wallpaper groups** — assign multiple monitors to a group; one logical wallpaper
  per group
- **Tag system** — group-specific tags, fallback tags, global fallbacks, random
  selection with weighted extension point
- **Recommendation engine** — deterministic, configurable scoring
  (resolution, aspect ratio, quality, relevance, popularity, editor's choice, novelty)
- **Local cache** with duplicate prevention, SQLite history, and manual cleanup
- **Automatic rotation** — Disabled, 15/30 min, 1/2/4/6/12 h, Daily, Custom,
  Specific time
- **Resilient scheduler** — survives close/minimize, handles sleep/resume, no
  duplicate concurrent rotations
- **System tray** — Open, New Wallpaper, Settings, Exit; balloon notifications
- **Windows startup** (no admin) via HKCU Run key
- **Secure API-key storage** with DPAPI
- **Manual controls** — New Wallpaper, Pause/Resume, Apply Selected
- **History** with favorites and 1–5 star ratings
- **Search preview** — test current Pixabay configuration
- **Modern UI** — dark/light/system theme, Fluent-inspired styling, high-DPI

---

## Installation

The application is distributed as a **self-contained portable executable** —
no installer and no .NET runtime required on the target machine.

```powershell
dotnet publish -c Release -r win-x64 --self-contained true
```

The output goes to `bin\Release\net10.0-windows\win-x64\publish\NTWallpaper.exe`.
Copy the entire `publish` folder anywhere and run `NTWallpaper.exe`.

### First launch

1. The app starts minimized to the system tray (double-click the tray icon to open).
2. Open **Settings** and paste your Pixabay API key.
3. Open **Groups** to create a group and assign monitors to it.
4. Open **Tags** via the **Settings** / Groups interface to add interests.
5. Adjust rotation interval and start. Done.

---

## Pixabay API key

Get a free key at <https://pixabay.com/api/docs/> and paste it into **Settings**.
The key is encrypted with DPAPI (CurrentUser scope) and stored in
`%LOCALAPPDATA%\PixabayWallpaper\secrets\`. It is never written to disk in
plaintext, logs, or exception messages.

---

## Configuration

### Groups

- **Create** a group, set rotation interval (or a specific time of day),
- **Assign targets** (monitors, lock screen) to a group,
- **Tags** per group + fallback tags; global fallback tags are used when a
  group has no tags.

### Rotation

| Preset            | Value          |
|-------------------|----------------|
| Disabled          | no rotation    |
| 15 minutes / 30 minutes | quick demo |
| 1 / 2 / 4 / 6 / 12 hours   | common cadence |
| Daily             | once per day   |
| Custom            | any interval   |
| Specific time     | e.g. 08:00     |

The scheduler fires one rotation per group at a time; missed rotations
(after sleep) trigger at most one catch-up run.

### Storage

| Item                | Location                                      |
|---------------------|-----------------------------------------------|
| Database            | `%LOCALAPPDATA%\PixabayWallpaper\ntwallpaper.db` |
| Cached wallpapers   | `%LOCALAPPDATA%\PixabayWallpaper\Wallpapers\` (configurable) |
| API key (encrypted) | `%LOCALAPPDATA%\PixabayWallpaper\secrets\`   |
| Logs                | `%LOCALAPPDATA%\PixabayWallpaper\logs\`       |

Cleanup is **manual** via the History page. Favorites are never deleted.

---

## Windows Startup

`Settings → Start with Windows` writes a value to
`HKCU\Software\Microsoft\Windows\CurrentVersion\Run` pointing to the running
executable. **No administrator privileges required.**

`Start minimized to tray` keeps the main window hidden at boot.

---

## Lock Screen

Lock-screen support is implemented via the Windows Personalization CSP
registry (`HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\PersonalizationCSP`).
Because Windows does **not** expose a per-user API for the lock screen image,
this feature requires **administrator privileges** and Windows 10/11
Pro/Enterprise. `ILockScreenService.IsSupported` returns `true` only when
the process is elevated; otherwise the service reports a clear "not supported"
message and never claims success. The main window and tray reflect real
support status.

---

## Virtual Desktops

Per-virtual-desktop wallpapers are **not** supported by any documented Windows
API (only undocumented internals exist). The application exposes the
`IVirtualDesktopService` interface and reports `IsSupported = false` with a
clear explanation rather than faking support. This is a known Windows
platform limitation.

---

## Architecture

```text
Pixabay API
    ↓
PixabayImageProvider (retry / backoff)
    ↓
RecommendationService
    ↓
CacheService → ImageRepository (SQLite via Dapper)
    ↓
ImageRenderingService (Span composition)
    ↓
WallpaperService (IDesktopWallpaper COM)
    ↓
Windows desktop / lock screen
```

`WallpaperOrchestrator` owns the scheduling loop, handles per-group
serialization, target → group assignment, monitor hot-plug detection
(`Microsoft.Win32.SystemEvents.DisplaySettingsChanged`), and PowerModeChanged
sleep/resume.

---

## Development

### Prerequisites

- .NET 10 SDK (installed)
- Windows 10 1809 (build 17763) or later for development
- **Windows SDK 10.0.26100** recommended; without it the SDK falls back to
  `TargetPlatformVersion=7.0` and the WPF markup-compile temp project
  fails. See **Build notes** below.

### Build

```powershell
dotnet build -c Debug
```

### Run

```powershell
dotnet run -c Debug
```

### Test

```powershell
dotnet test NTWallpaper.Tests
```

### Publish (portable)

```powershell
dotnet publish -c Release -r win-x64 --self-contained true
```

### Build notes

If you see `NETSDK1135: SupportedOSPlatformVersion … cannot be higher than
TargetPlatformVersion 7.0`, the Windows SDK is not installed in Program
Files and `GetTargetPlatformVersion` returns `7.0`. Two workarounds ship
with this repo:

1. `Directory.Build.props` pins `net10.0-windows` + `TargetPlatformVersion=10.0.26100.0`
2. `Directory.Build.targets` re-pins `TargetPlatformVersion` *after* the SDK's
   inference reset (this is what makes the build resolve to the correct SDK
   version).

If the WPF markup-compile temp project (`_wpftmp`) fails with
`Application is a namespace but is used like a type` + `HttpClient could not
be found`, install the Windows 11 SDK (10.0.26100.x) so the
`Microsoft.Windows.SDK.NET.Ref` targeting pack is available. As a last
resort, change the SDK attribute to
`<Project Sdk="Microsoft.NET.Sdk.WindowsDesktop">`.

---

## Logging

Structured logs (Serilog) at
`%LOCALAPPDATA%\PixabayWallpaper\logs\ntwallpaper-YYYYMMDD.log`, rolling daily.

- API keys are never logged.
- Errors are reported via the system tray balloon and the notification log.

---

## License

Pixabay content is subject to the [Pixabay License](https://pixabay.com/service/license/).
The Pixabay API key you provide is used only to search and download
images from your own account; no data is sent to any other service.
