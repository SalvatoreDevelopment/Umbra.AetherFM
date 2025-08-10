# AetherFM Plugin for Umbra

A toolbar widget that controls the AetherFM radio plugin via formal IPC gates. It provides quick Play/Stop, volume control, and favorites access directly from the toolbar.

This widget integrates with the official AetherFM Dalamud plugin by SalvatoreDevelopment. See the plugin repository for details and releases: [AetherFM (Dalamud plugin)](https://github.com/SalvatoreDevelopment/AetherFM).

## Features
- Play/Stop toggle with first‑class support for Toggle/Resume.
- Volume controls (±) that do not close the popup, with live percentage display.
- Open Window / Toggle Mini Player shortcuts.
- Favorites (by name): quick launch from the menu and add/remove current station.
- Clear status label on the toolbar (e.g., `Playing: Station Name`).
- Startup‑safe: never auto‑resumes playback on widget/game restart.

## How to Install
Open Umbra Settings
Navigate to Plugins
Enter **SalvatoreDevelopment** as repo owner and **Umbra.AetherFM** as repo name and click the install button.

## Usage
- Click the widget to open the popup.
- Use the top action to toggle Play/Stop (label updates immediately).
- Adjust volume with "Volume + / Volume −"; the popup stays open, and the percentage updates.
- Use "Open Window" or "Toggle Mini Player" to control AetherFM UI.
- In the Favorites section, click a station name to start it; use the contextual action to add/remove the current station.

## License
MIT
