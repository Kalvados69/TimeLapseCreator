# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

TimeLapseCreator is a small Windows desktop app (.NET Framework 4.6.1, WPF) that turns a folder of JPEG photos into a time-lapse MP4 by encoding a subset of frames with FFmpeg (via the Accord.NET video wrapper). There is no README, no test project, and no CI in this repo — this file is the only project documentation.

## Build

Windows + MSBuild/Visual Studio only (WPF + a native FFmpeg-backed video writer).

```
nuget restore TimeLapseCreator.sln
msbuild TimeLapseCreator.sln /p:Configuration=Release
```

NuGet dependencies (see `TimeLapseCreator/packages.config`): `Accord`, `Accord.Video`, `Accord.Video.FFMPEG` (video encoding), `Microsoft-WindowsAPICodePack-Core`/`-Shell` (the Vista+ folder-picker dialog). The `Accord.Video.FFMPEG` reference is `processorArchitecture=x86` — if you hit a `BadImageFormatException` at run time, build/run as x86 rather than AnyCPU.

There are no tests, no lint config, and no CI workflow for this repo. Open `TimeLapseCreator.sln` in Visual Studio for interactive debugging (F5 launches the window).

## Architecture

Entry point: `App.xaml`/`App.xaml.cs` → `MainWindow`.

- **`TimelapseProject.cs`** — the one plain model class, implementing `INotifyPropertyChanged`. Set as `MainWindow`'s `DataContext` and bound directly from XAML. Holds `SourcePath`, `Framerate`, `ImagesUsageRate`, and derives `SourceImages`/`SourceImagesCount`/`UsedImagesCount`/`EstimatedTimelapseDuration` reactively — changing `SourcePath` fires property-changed notifications for every dependent computed property.
- **`MainWindow.xaml` / `MainWindow.xaml.cs`** — everything else lives here as code-behind; there is no separate service/ViewModel layer for the actual work:
  - Folder selection via `Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog`.
  - `RefreshPreview` re-scans the source folder (`GetImageFiles`, matching `*.jpg`/`*.jpeg` case-insensitively) and updates the live preview/estimate as the user edits framerate or usage-rate fields (wired via `TimelapseParametersChanged`).
  - Rendering runs on a `BackgroundWorker` (`BackgroundRenderTimelapse` → `bw_DoWork`) so the UI thread stays responsive; progress is reported via `worker.ReportProgress` and reflected in a progress bar. `bw_DoWork` walks the source images in steps of `ImagesUsageRate`, writing each sampled frame into an `Accord.Video.FFMPEG.VideoFileWriter` opened lazily on the first frame (so frame size comes from the first image). Output is always written as **`EasyTimelapse.mp4` inside the selected source folder** — there is no output-path picker.
- **`TimelapseProject.SourceImages`** currently only matches `*.JPG` (case handled by the OS on Windows but not portable), while `MainWindow.GetImageFiles` matches `jpg`/`jpeg` case-insensitively — these two "find the source images" implementations are independent and can disagree; don't assume fixing one fixes the other.

## Repo quirks worth knowing before editing

- `btnRenderTimelapse_Click` contains a large block of commented-out code that duplicates the synchronous (non-background-worker) version of the render logic, marked with French toggle comments (`// A RECOMMENTER` / `// A DECOMMENTER`, i.e. "re-comment this" / "uncomment this"). It's dead code kept for reference, not an alternate code path to wire up — the active path is `BackgroundRenderTimelapse()`.
- UI strings and comments are in French (e.g. "Rechercher le dossier", `Logique d'interaction pour MainWindow.xaml`).
- Several UI label updates (`lblDetectedImagesNumber`, `lblUsedImagesNumber`, `lblOutputVideoEstimatedDurationNumber`) are commented out in `RefreshPreview`/the dead render block; check the current `MainWindow.xaml` bindings before assuming a label is driven from code-behind vs. XAML binding to `TimelapseProject`.
