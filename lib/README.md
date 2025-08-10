# Required DLL Files

This folder should contain the following DLL files for building the project when Umbra is not installed locally:

- `Umbra.dll` - Core Umbra framework
- `Umbra.Common.dll` - Umbra common utilities  
- `Una.Drawing.dll` - Una drawing primitives
- `Dalamud.dll` - Dalamud plugin interface
- `FFXIVClientStructs.dll` - FFXIV client structures

## How to obtain these files:

1. **From local Umbra installation** (if available):
   - Copy from `%APPDATA%\XIVLauncher\installedPlugins\Umbra\[version]\`
   - Copy from `%APPDATA%\XIVLauncher\addon\Hooks\dev\`

2. **From Umbra releases**:
   - Download from [Umbra releases](https://github.com/una-xiv/Umbra/releases)
   - Extract the required DLL files

3. **From Dalamud releases**:
   - Download from [Dalamud releases](https://github.com/goatcorp/Dalamud/releases)
   - Extract the required DLL files

## Note for CI builds:
These files are only needed when building outside of a local FFXIV/Umbra environment.
For local development, the project will automatically use the installed versions.
