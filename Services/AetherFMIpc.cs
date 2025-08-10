using System;
using System.Collections.Generic;
using Dalamud.Plugin;

namespace Umbra.AetherFM.Services;

/// <summary>
/// Complete wrapper for AetherFM formal IPC gates (API v1).
/// Error policy: never throw across IPC boundaries; return safe fallbacks.
/// </summary>
public sealed class AetherFMIpc : IDisposable
{
    private readonly IDalamudPluginInterface _pi;
    private readonly HashSet<Action<string>> _subs = new();

    public AetherFMIpc(IDalamudPluginInterface pluginInterface)
    {
        _pi = pluginInterface ?? throw new ArgumentNullException(nameof(pluginInterface));
        Console.WriteLine($"[AetherFMIpc] Initialized with plugin interface");
    }

    // ---------- Versioning & Health ----------
    public int  IpcVersion()  => SafeInvoke(() => _pi.GetIpcSubscriber<int>("AetherFM.IpcVersion").InvokeFunc(), 0, "IpcVersion");
    public int  FeatureFlags()=> SafeInvoke(() => _pi.GetIpcSubscriber<int>("AetherFM.FeatureFlags").InvokeFunc(), 0, "FeatureFlags");
    public bool IsReady()     => SafeInvoke(() => _pi.GetIpcSubscriber<bool>("AetherFM.IsReady").InvokeFunc(), false, "IsReady");
    public bool IsAvailable() => IsReady() && IpcVersion() >= 1;

    // ---------- State & Info ----------
    public string GetCurrentStation()    => SafeInvoke(() => _pi.GetIpcSubscriber<string>("AetherFM.GetCurrentStation").InvokeFunc() ?? string.Empty, string.Empty, "GetCurrentStation");
    public string GetCurrentStationUrl() => SafeInvoke(() => _pi.GetIpcSubscriber<string>("AetherFM.GetCurrentStationUrl").InvokeFunc() ?? string.Empty, string.Empty, "GetCurrentStationUrl");
    public string GetStatus()            => SafeInvoke(() => _pi.GetIpcSubscriber<string>("AetherFM.GetStatus").InvokeFunc() ?? string.Empty, string.Empty, "GetStatus");

    // ---------- Controls & UI ----------
    public bool Play()            => SafeInvoke(() => _pi.GetIpcSubscriber<bool>("AetherFM.Play").InvokeFunc(), false, "Play");
    public bool Pause()           => SafeInvoke(() => _pi.GetIpcSubscriber<bool>("AetherFM.Pause").InvokeFunc(), false, "Pause");
    public bool Stop()            => SafeInvoke(() => _pi.GetIpcSubscriber<bool>("AetherFM.Stop").InvokeFunc(), false, "Stop");
    public bool ResumeLast()      => SafeInvoke(() => _pi.GetIpcSubscriber<bool>("AetherFM.ResumeLast").InvokeFunc(), false, "ResumeLast");
    public bool TogglePlayStop()  => SafeInvoke(() => _pi.GetIpcSubscriber<bool>("AetherFM.TogglePlayStop").InvokeFunc(), false, "TogglePlayStop");
    public bool OpenWindow()      => SafeInvoke(() => _pi.GetIpcSubscriber<bool>("AetherFM.OpenWindow").InvokeFunc(), false, "OpenWindow");
    public bool ToggleWindow()    => SafeInvoke(() => _pi.GetIpcSubscriber<bool>("AetherFM.ToggleWindow").InvokeFunc(), false, "ToggleWindow");
    public bool OpenMiniPlayer()  => SafeInvoke(() => _pi.GetIpcSubscriber<bool>("AetherFM.OpenMiniPlayer").InvokeFunc(), false, "OpenMiniPlayer");
    public bool ToggleMiniPlayer()=> SafeInvoke(() => _pi.GetIpcSubscriber<bool>("AetherFM.ToggleMiniPlayer").InvokeFunc(), false, "ToggleMiniPlayer");

    // ---------- Direct Play ----------
    public bool PlayByUrl(string url)   => SafeInvoke(() => _pi.GetIpcSubscriber<string, bool>("AetherFM.PlayByUrl").InvokeFunc(url ?? string.Empty), false, "PlayByUrl");
    public bool PlayByName(string name) => SafeInvoke(() => _pi.GetIpcSubscriber<string, bool>("AetherFM.PlayByName").InvokeFunc(name ?? string.Empty), false, "PlayByName");

    // ---------- Favorites ----------
    public string[] GetFavorites()      => SafeInvoke(() => _pi.GetIpcSubscriber<string[]?>("AetherFM.GetFavorites").InvokeFunc() ?? Array.Empty<string>(), Array.Empty<string>(), "GetFavorites");
    public string[] GetFavoriteNames()
    {
        // Prefer official gate if available; fall back to alternative naming; otherwise empty
        var names = SafeInvoke(() => _pi.GetIpcSubscriber<string[]?>("AetherFM.GetFavoriteNames").InvokeFunc() ?? Array.Empty<string>(), Array.Empty<string>(), "GetFavoriteNames");
        if (names.Length == 0)
        {
            names = SafeInvoke(() => _pi.GetIpcSubscriber<string[]?>("AetherFM.GetFavoritesNames").InvokeFunc() ?? Array.Empty<string>(), Array.Empty<string>(), "GetFavoritesNames");
        }
        return names;
    }
    public bool     AddFavorite(string url) => SafeInvoke(() => _pi.GetIpcSubscriber<string, bool>("AetherFM.AddFavorite").InvokeFunc(url ?? string.Empty), false, "AddFavorite");
    public bool     RemoveFavorite(string url) => SafeInvoke(() => _pi.GetIpcSubscriber<string, bool>("AetherFM.RemoveFavorite").InvokeFunc(url ?? string.Empty), false, "RemoveFavorite");

    // ---------- Volume ----------
    public float GetVolume() => SafeInvoke(() => _pi.GetIpcSubscriber<float>("AetherFM.GetVolume").InvokeFunc(), 0f, "GetVolume");
    public bool  SetVolume(float value) => SafeInvoke(() => _pi.GetIpcSubscriber<float, bool>("AetherFM.SetVolume").InvokeFunc(Math.Clamp(value, 0f, 1f)), false, "SetVolume");

    // ---------- Status Events ----------
    public bool SubscribeStatusChanged(Action<string> cb)
    {
        if (cb == null) return false;
        var ok = SafeInvoke(() => _pi.GetIpcSubscriber<Action<string>, bool>("AetherFM.SubscribeStatusChanged").InvokeFunc(cb), false, "SubscribeStatusChanged");
        if (ok) _subs.Add(cb);
        return ok;
    }

    public bool UnsubscribeStatusChanged(Action<string> cb)
    {
        if (cb == null) return false;
        var ok = SafeInvoke(() => _pi.GetIpcSubscriber<Action<string>, bool>("AetherFM.UnsubscribeStatusChanged").InvokeFunc(cb), false, "UnsubscribeStatusChanged");
        if (ok) _subs.Remove(cb);
        return ok;
    }

    // ---------- State aggregate (snapshot) ----------
    public AetherFMState GetStateUtc()
    {
        try
        {
            var now   = DateTime.UtcNow;
            var ready = IsAvailable();
            var st    = GetStatus();
            var name  = GetCurrentStation();
            var url   = GetCurrentStationUrl();
            var vol   = Math.Clamp(GetVolume(), 0f, 1f);

            return new AetherFMState(ready, st, name, url, vol, now);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AetherFMIpc] Error getting AetherFM state: {ex.Message}");
            return AetherFMState.Unavailable(DateTime.UtcNow);
        }
    }

    private static T SafeInvoke<T>(Func<T> func, T fallback, string operationName = "Unknown")
    {
        try 
        { 
            var result = func();
            Console.WriteLine($"[AetherFMIpc] IPC operation {operationName} completed successfully");
            return result; 
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AetherFMIpc] IPC operation {operationName} failed: {ex.Message}");
            return fallback;
        }
    }

    public void Dispose()
    {
        try
        {
            Console.WriteLine($"[AetherFMIpc] Disposing AetherFMIpc service");
            
            foreach (var cb in _subs) 
            { 
                try 
                { 
                    UnsubscribeStatusChanged(cb); 
                    Console.WriteLine($"[AetherFMIpc] Unsubscribed status change callback");
                } 
                catch (Exception ex)
                {
                    Console.WriteLine($"[AetherFMIpc] Error unsubscribing status change callback: {ex.Message}");
                } 
            }
            
            _subs.Clear();
            Console.WriteLine($"[AetherFMIpc] AetherFMIpc service disposed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AetherFMIpc] Error during AetherFMIpc disposal: {ex.Message}");
        }
    }
}


