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
        // Instantiate IPC when the framework is ready
        _ipc = new AetherFMIpc(Framework.DalamudPlugin);

        // Prime the last known station URL from the current source (if any)
        _lastUrl = _ipc.GetCurrentStationUrl();

        // Update cached station whenever status becomes Playing
        _ipc.SubscribeStatusChanged(s =>
        {
            if (string.Equals(s, "Playing", StringComparison.OrdinalIgnoreCase))
            {
                var url = _ipc.GetCurrentStationUrl();
                if (!string.IsNullOrEmpty(url)) _lastUrl = url;
                var name = _ipc.GetCurrentStation();
                if (!string.IsNullOrEmpty(name)) _lastName = name;
                // clear any local caches if needed (no-op)
                return;
            }

            if (string.Equals(s, "Stopped", StringComparison.OrdinalIgnoreCase))
            {
                // Never auto-resume on Stopped; only user action can start playback
                return;
            }
        });

        // Rebuild menu when popup opens (refresh local cache first)
        Popup.OnPopupOpen += () =>
        {
            if (_ipc is null) return;
            _lastUrl  = _ipc.GetCurrentStationUrl();
            _lastName = _ipc.GetCurrentStation();
            BuildPopup();
        };
    }

    private void BuildPopup(bool? forceIsPlaying = null)
    {
        if (_ipc is null) return;

        Popup.Clear();

        var state = _ipc.GetStateUtc();
        var isPlaying = forceIsPlaying ?? state.Status.Equals("Playing", StringComparison.OrdinalIgnoreCase);

        Popup.Add(new MenuPopup.Button(isPlaying ? "Stop" : "Play") {
            Icon    = isPlaying ? FontAwesomeIcon.Stop : FontAwesomeIcon.Play,
            ClosePopupOnClick = false,
            OnClick = () =>
            {
                if (_ipc is null) return;
                if (_ipc.TogglePlayStop()) { BuildPopup(!isPlaying); return; }
                if (!isPlaying && _ipc.ResumeLast()) { BuildPopup(true); return; }
                // No further fallbacks by design
                BuildPopup(isPlaying);
            }
        });

        Popup.Add(new MenuPopup.Separator());
        // Show current volume value
        var volPct = (int)Math.Round((_ipc.GetVolume()) * 100f);
        Popup.Add(new MenuPopup.Header($"Volume: {volPct}%"));

        // Show Volume+ first, then Volume-
        Popup.Add(new MenuPopup.Button("Volume +") {
            Icon    = FontAwesomeIcon.VolumeUp,
            ClosePopupOnClick = false,
            OnClick = () => { var v = (_ipc?.GetVolume() ?? 0f) + 0.05f; _ipc?.SetVolume(v); BuildPopup(); }
        });

        Popup.Add(new MenuPopup.Button("Volume -") {
            Icon    = FontAwesomeIcon.VolumeDown,
            ClosePopupOnClick = false,
            OnClick = () => { var v = (_ipc?.GetVolume() ?? 0f) - 0.05f; _ipc?.SetVolume(v); BuildPopup(); }
        });

        Popup.Add(new MenuPopup.Separator());

        Popup.Add(new MenuPopup.Button("Open Window") {
            Icon    = FontAwesomeIcon.WindowMaximize,
            ClosePopupOnClick = false,
            OnClick = () => { _ipc?.OpenWindow(); BuildPopup(); }
        });

        Popup.Add(new MenuPopup.Button("Toggle Mini Player") {
            Icon    = FontAwesomeIcon.CompactDisc,
            ClosePopupOnClick = false,
            OnClick = () => { _ipc?.ToggleMiniPlayer(); BuildPopup(); }
        });

        // Favorites section (by name)
        Popup.Add(new MenuPopup.Separator());
        Popup.Add(new MenuPopup.Header("Favorites"));

        var favNames = _ipc.GetFavoriteNames();
        var currentNameFav = _ipc.GetCurrentStation();

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
                    OnClick = () => { if (!string.IsNullOrEmpty(favName)) { _ipc.PlayByName(favName); _lastName = favName; BuildPopup(true); } }
                });
            }
        }
        // Add/Remove current station to/from favorites by URL (IPC supports URL keys)
        var currentUrlFav = _ipc.GetCurrentStationUrl();
        if (!string.IsNullOrEmpty(currentUrlFav))
        {
            var allUrls = _ipc.GetFavorites();
            var isFav = Array.Exists(allUrls, u => string.Equals(u, currentUrlFav, StringComparison.OrdinalIgnoreCase));
            if (!isFav)
            {
                Popup.Add(new MenuPopup.Button("Add current to favorites") {
                    Icon = FontAwesomeIcon.Star,
                    ClosePopupOnClick = false,
                    OnClick = () => { if (_ipc.AddFavorite(currentUrlFav)) BuildPopup(isPlaying); }
                });
            }
            else
            {
                Popup.Add(new MenuPopup.Button("Remove current from favorites") {
                    Icon = FontAwesomeIcon.Trash,
                    ClosePopupOnClick = false,
                    OnClick = () => { if (_ipc.RemoveFavorite(currentUrlFav)) BuildPopup(isPlaying); }
                });
            }
        }
    }

    // Legacy resume fallback removed (AetherFM v1.2): using only TogglePlayStop/ResumeLast

    protected override void OnDraw()
    {
        var ready = _ipc?.IsAvailable() ?? false;
        var label = "AetherFM non disponibile";

        if (ready)
        {
            var st = _ipc!.GetStatus();
            var name = _ipc!.GetCurrentStation();
            if (string.IsNullOrEmpty(name) && st.Equals("Playing", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(_lastName))
            {
                name = _lastName;
            }
            label = string.IsNullOrEmpty(name) ? st : $"{st}: {name}";
        }

        SetText(label);
        SetDisabled(!ready);
    }
}


