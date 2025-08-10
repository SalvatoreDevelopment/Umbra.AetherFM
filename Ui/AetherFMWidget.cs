using System;
using Umbra.AetherFM.Services;
using Umbra.Widgets;
using Umbra.Common;
using Dalamud.Interface;

namespace Umbra.AetherFM.Ui;

/// <summary>
/// Toolbar widget for AetherFM with a popup.
/// - Menu popup provides actions (Play/Stop, Volume, Open Window, Toggle Mini Player)
/// Auto discovery via [ToolbarWidget] + StandardToolbarWidget subclass.
/// </summary>
[ToolbarWidget(
    "AetherFM.Controller",
    "AetherFM",
    "Controllo rapido di AetherFM (Play/Stop/Volume/Windows)",
    new [] { "aetherfm", "radio", "music", "stream" }
)]
internal sealed class AetherFMWidget(
    WidgetInfo                  info,
    string?                     guid         = null,
    System.Collections.Generic.Dictionary<string, object>? configValues = null
) : StandardToolbarWidget(info, guid, configValues)
{
    private AetherFMIpc? _ipc;
    private string? _lastUrl;
    private string? _lastName;

    public override MenuPopup Popup { get; } = new();

    protected override StandardWidgetFeatures Features =>
        StandardWidgetFeatures.Text | StandardWidgetFeatures.Icon;

    protected override string DefaultIconType => IconTypeFontAwesome;
    protected override FontAwesomeIcon DefaultFontAwesomeIcon => FontAwesomeIcon.Music;

    protected override void OnLoad()
    {
        try
        {
            Console.WriteLine("[AetherFMWidget] Initializing AetherFM Widget");
            
            // Instantiate IPC when the framework is ready
            _ipc = new AetherFMIpc(Framework.DalamudPlugin);

            // Prime the last known station URL from the current source (if any)
            _lastUrl = _ipc.GetCurrentStationUrl();
            Console.WriteLine($"[AetherFMWidget] Initial station URL: {_lastUrl ?? "null"}");

            // Update cached station whenever status becomes Playing
            _ipc.SubscribeStatusChanged(s =>
            {
                try
                {
                    if (string.Equals(s, "Playing", StringComparison.OrdinalIgnoreCase))
                    {
                        var url = _ipc.GetCurrentStationUrl();
                        if (!string.IsNullOrEmpty(url)) 
                        {
                            _lastUrl = url;
                            Console.WriteLine($"[AetherFMWidget] Station started playing: {url}");
                        }
                        
                        var name = _ipc.GetCurrentStation();
                        if (!string.IsNullOrEmpty(name)) 
                        {
                            _lastName = name;
                            Console.WriteLine($"[AetherFMWidget] Station name updated: {name}");
                        }
                        return;
                    }

                    if (string.Equals(s, "Stopped", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("[AetherFMWidget] Station stopped");
                        // Never auto-resume on Stopped; only user action can start playback
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AetherFMWidget] Error in status change callback for status {s}: {ex.Message}");
                }
            });

            // Rebuild menu when popup opens (refresh local cache first)
            Popup.OnPopupOpen += () =>
            {
                try
                {
                    if (_ipc is null) return;
                    _lastUrl  = _ipc.GetCurrentStationUrl();
                    _lastName = _ipc.GetCurrentStation();
                    Console.WriteLine($"[AetherFMWidget] Popup opened, refreshed cache - URL: {_lastUrl}, Name: {_lastName}");
                    BuildPopup();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AetherFMWidget] Error refreshing popup cache: {ex.Message}");
                }
            };
            
            Console.WriteLine("[AetherFMWidget] AetherFM Widget initialized successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AetherFMWidget] Failed to initialize AetherFM Widget: {ex.Message}");
            throw;
        }
    }

    private void BuildPopup(bool? forceIsPlaying = null)
    {
        try
        {
            if (_ipc is null)
            {
                Console.WriteLine("[AetherFMWidget] Cannot build popup: IPC service is null");
                return;
            }

            Popup.Clear();
            Console.WriteLine("[AetherFMWidget] Building popup menu");

            var state = _ipc.GetStateUtc();
            var isPlaying = forceIsPlaying ?? state.IsPlaying;
            Console.WriteLine($"[AetherFMWidget] Current state - Status: {state.Status}, IsPlaying: {isPlaying}");

            // Play/Stop button
            Popup.Add(new MenuPopup.Button(isPlaying ? "Stop" : "Play") {
                Icon    = isPlaying ? FontAwesomeIcon.Stop : FontAwesomeIcon.Play,
                ClosePopupOnClick = false,
                OnClick = () =>
                {
                    try
                    {
                        if (_ipc is null) return;
                        if (_ipc.TogglePlayStop()) 
                        { 
                            Console.WriteLine("[AetherFMWidget] TogglePlayStop successful, rebuilding popup");
                            BuildPopup(!isPlaying); 
                            return; 
                        }
                        if (!isPlaying && _ipc.ResumeLast()) 
                        { 
                            Console.WriteLine("[AetherFMWidget] ResumeLast successful, rebuilding popup");
                            BuildPopup(true); 
                            return; 
                        }
                        // No further fallbacks by design
                        Console.WriteLine("[AetherFMWidget] No fallback action available, rebuilding popup with current state");
                        BuildPopup(isPlaying);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[AetherFMWidget] Error in Play/Stop button click handler: {ex.Message}");
                    }
                }
            });

            Popup.Add(new MenuPopup.Separator());
            
            // Volume controls
            var volPct = state.VolumePercentage;
            Popup.Add(new MenuPopup.Header($"Volume: {volPct}%"));

            Popup.Add(new MenuPopup.Button("Volume +") {
                Icon    = FontAwesomeIcon.VolumeUp,
                ClosePopupOnClick = false,
                OnClick = () => 
                { 
                    try
                    {
                        var currentVol = _ipc?.GetVolume() ?? 0f;
                        var newVol = Math.Clamp(currentVol + 0.05f, 0f, 1f);
                        if (_ipc?.SetVolume(newVol) == true)
                        {
                            Console.WriteLine($"[AetherFMWidget] Volume increased from {currentVol} to {newVol}");
                        }
                        BuildPopup(); 
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[AetherFMWidget] Error increasing volume: {ex.Message}");
                    }
                }
            });

            Popup.Add(new MenuPopup.Button("Volume -") {
                Icon    = FontAwesomeIcon.VolumeDown,
                ClosePopupOnClick = false,
                OnClick = () => 
                { 
                    try
                    {
                        var currentVol = _ipc?.GetVolume() ?? 0f;
                        var newVol = Math.Clamp(currentVol - 0.05f, 0f, 1f);
                        if (_ipc?.SetVolume(newVol) == true)
                        {
                            Console.WriteLine($"[AetherFMWidget] Volume decreased from {currentVol} to {newVol}");
                        }
                        BuildPopup(); 
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[AetherFMWidget] Error decreasing volume: {ex.Message}");
                    }
                }
            });

            Popup.Add(new MenuPopup.Separator());

            // Window controls
            Popup.Add(new MenuPopup.Button("Open Window") {
                Icon    = FontAwesomeIcon.WindowMaximize,
                ClosePopupOnClick = false,
                OnClick = () => 
                { 
                    try
                    {
                        if (_ipc?.OpenWindow() == true)
                        {
                            Console.WriteLine("[AetherFMWidget] Window opened successfully");
                        }
                        BuildPopup(); 
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[AetherFMWidget] Error opening window: {ex.Message}");
                    }
                }
            });

            Popup.Add(new MenuPopup.Button("Toggle Mini Player") {
                Icon    = FontAwesomeIcon.CompactDisc,
                ClosePopupOnClick = false,
                OnClick = () => 
                { 
                    try
                    {
                        if (_ipc?.ToggleMiniPlayer() == true)
                        {
                            Console.WriteLine("[AetherFMWidget] Mini player toggled successfully");
                        }
                        BuildPopup(); 
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[AetherFMWidget] Error toggling mini player: {ex.Message}");
                    }
                }
            });

            // Favorites section
            Popup.Add(new MenuPopup.Separator());
            Popup.Add(new MenuPopup.Header("Favorites"));

            var favNames = _ipc.GetFavoriteNames();
            var currentNameFav = _ipc.GetCurrentStation();
            Console.WriteLine($"[AetherFMWidget] Favorites count: {favNames.Length}, Current station: {currentNameFav ?? "null"}");

            if (favNames.Length == 0)
            {
                Popup.Add(new MenuPopup.Header("(empty)"));
            }
            else
            {
                foreach (var favName in favNames)
                {
                    var label = string.IsNullOrEmpty(favName) ? "(invalid)" : favName;
                    Popup.Add(new MenuPopup.Button(label) {
                        Icon = FontAwesomeIcon.Star,
                        ClosePopupOnClick = false,
                        OnClick = () => 
                        { 
                            try
                            {
                                if (!string.IsNullOrEmpty(favName)) 
                                { 
                                    if (_ipc.PlayByName(favName))
                                    {
                                        _lastName = favName;
                                        Console.WriteLine($"[AetherFMWidget] Started playing favorite: {favName}");
                                        BuildPopup(true); 
                                    }
                                    else
                                    {
                                        Console.WriteLine($"[AetherFMWidget] Failed to play favorite: {favName}");
                                    }
                                } 
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[AetherFMWidget] Error playing favorite: {favName}: {ex.Message}");
                            }
                        }
                    });
                }
            }
            
            // Add/Remove current station to/from favorites
            var currentUrlFav = _ipc.GetCurrentStationUrl();
            if (!string.IsNullOrEmpty(currentUrlFav))
            {
                var allUrls = _ipc.GetFavorites();
                var isFav = Array.Exists(allUrls, u => string.Equals(u, currentUrlFav, StringComparison.OrdinalIgnoreCase));
                Console.WriteLine($"[AetherFMWidget] Current station URL: {currentUrlFav}, IsFavorite: {isFav}");
                
                if (!isFav)
                {
                    Popup.Add(new MenuPopup.Button("Add current to favorites") {
                        Icon = FontAwesomeIcon.Star,
                        ClosePopupOnClick = false,
                        OnClick = () => 
                        { 
                            try
                            {
                                if (_ipc.AddFavorite(currentUrlFav))
                                {
                                    Console.WriteLine($"[AetherFMWidget] Added station to favorites: {currentUrlFav}");
                                    BuildPopup(isPlaying); 
                                }
                                else
                                {
                                    Console.WriteLine($"[AetherFMWidget] Failed to add station to favorites: {currentUrlFav}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[AetherFMWidget] Error adding station to favorites: {currentUrlFav}: {ex.Message}");
                            }
                        }
                    });
                }
                else
                {
                    Popup.Add(new MenuPopup.Button("Remove current from favorites") {
                        Icon = FontAwesomeIcon.Trash,
                        ClosePopupOnClick = false,
                        OnClick = () => 
                        { 
                            try
                            {
                                if (_ipc.RemoveFavorite(currentUrlFav))
                                {
                                    Console.WriteLine($"[AetherFMWidget] Removed station from favorites: {currentUrlFav}");
                                    BuildPopup(isPlaying); 
                                }
                                else
                                {
                                    Console.WriteLine($"[AetherFMWidget] Failed to remove station from favorites: {currentUrlFav}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[AetherFMWidget] Error removing station from favorites: {currentUrlFav}: {ex.Message}");
                            }
                        }
                    });
                }
            }
            
            Console.WriteLine("[AetherFMWidget] Popup menu built successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AetherFMWidget] Error building popup menu: {ex.Message}");
        }
    }

    // Legacy resume fallback removed (AetherFM v1.2): using only TogglePlayStop/ResumeLast

    protected override void OnDraw()
    {
        try
        {
            var ready = _ipc?.IsAvailable() ?? false;
            var label = "AetherFM non disponibile";

            if (ready)
            {
                var state = _ipc!.GetStateUtc();
                var name = state.StationName;
                if (string.IsNullOrEmpty(name) && state.IsPlaying && !string.IsNullOrEmpty(_lastName))
                {
                    name = _lastName;
                    Console.WriteLine($"[AetherFMWidget] Using cached station name: {name}");
                }
                label = state.DisplayLabel;
                Console.WriteLine($"[AetherFMWidget] Widget label updated - Status: {state.Status}, Name: {name}, Label: {label}");
            }
            else
            {
                Console.WriteLine("[AetherFMWidget] AetherFM not available, widget disabled");
            }

            SetText(label);
            SetDisabled(!ready);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AetherFMWidget] Error in OnDraw: {ex.Message}");
            SetText("Errore AetherFM");
            SetDisabled(true);
        }
    }

    protected override void OnUnload()
    {
        try
        {
            Console.WriteLine("[AetherFMWidget] Unloading AetherFM Widget");
            
            // Clean up IPC service
            if (_ipc != null)
            {
                _ipc.Dispose();
                _ipc = null;
                Console.WriteLine("[AetherFMWidget] IPC service disposed");
            }
            
            Console.WriteLine("[AetherFMWidget] AetherFM Widget unloaded successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AetherFMWidget] Error during widget unload: {ex.Message}");
        }
    }
} 