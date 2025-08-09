# Umbra.AetherFM

An Umbra toolbar widget that controls the AetherFM radio plugin via formal IPC gates. It provides quick Play/Stop, volume control, and favorites access directly from the toolbar.

This widget integrates with the official AetherFM Dalamud plugin by SalvatoreDevelopment. See the plugin repository for details and releases: [AetherFM (Dalamud plugin)](https://github.com/SalvatoreDevelopment/AetherFM).

## Features
- Play/Stop toggle with first‑class support for Toggle/Resume.
- Volume controls (±) that do not close the popup, with live percentage display.
- Open Window / Toggle Mini Player shortcuts.
- Favorites (by name): quick launch from the menu and add/remove current station.
- Clear status label on the toolbar (e.g., `Playing: Station Name`).
- Startup‑safe: never auto‑resumes playback on widget/game restart.

## Requirements
- .NET 9 SDK
- Dalamud API Level 13 (runtime .NET 9)
- Umbra installed with custom plugin loading enabled
- AetherFM updated to the latest version from the repository above

## Installation (in‑game)
1. Build the widget (see below) so the DLL is available.
2. In FFXIV, open Umbra → Settings → Plugins → Add plugin.
3. Select `Umbra.AetherFM/out/Release/Umbra.AetherFM.dll`.
4. Add the widget to your toolbar and search for "AetherFM".

## Build
```bash
# From repository root
 dotnet build Umbra.AetherFM.sln -c Release
```
The build copies `Umbra.AetherFM.dll` to `Umbra.AetherFM/out/Release/` for easy import in Umbra.

## Usage
- Click the widget to open the popup.
- Use the top action to toggle Play/Stop (label updates immediately).
- Adjust volume with “Volume + / Volume −”; the popup stays open, and the percentage updates.
- Use “Open Window” or “Toggle Mini Player” to control AetherFM UI.
- In the Favorites section, click a station name to start it; use the contextual action to add/remove the current station.

## Development
- Sources live under `Umbra.AetherFM/`.
- Umbra/Dalamud references resolve from `%APPDATA%` paths (see the `.csproj`). Ensure Umbra and Dalamud are installed locally.
- Continuous Integration: GitHub Actions workflow in `.github/workflows/build.yml` builds on Windows and uploads Release artifacts from `out/Release/`.

## License
MIT
