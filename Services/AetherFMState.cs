using System;

namespace Umbra.AetherFM.Services;

/// <summary>
/// Immutable state snapshot of AetherFM at a specific point in time.
/// Provides safe access to current playback state and metadata.
/// </summary>
public readonly record struct AetherFMState(
    bool IsReady,
    string Status,
    string StationName,
    string StationUrl,
    float Volume01,
    DateTime LastUpdateUtc
)
{
    /// <summary>
    /// Creates an unavailable state snapshot for when AetherFM is not accessible.
    /// </summary>
    /// <param name="nowUtc">Current UTC timestamp</param>
    /// <returns>State representing unavailable AetherFM</returns>
    public static AetherFMState Unavailable(DateTime nowUtc) =>
        new(false, string.Empty, string.Empty, string.Empty, 0f, nowUtc);

    /// <summary>
    /// Gets the volume as a percentage (0-100).
    /// </summary>
    public int VolumePercentage => (int)Math.Round(Volume01 * 100f);

    /// <summary>
    /// Indicates if AetherFM is currently playing.
    /// </summary>
    public bool IsPlaying => IsReady && Status.Equals("Playing", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Indicates if AetherFM is currently stopped.
    /// </summary>
    public bool IsStopped => IsReady && Status.Equals("Stopped", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Indicates if AetherFM is currently paused.
    /// </summary>
    public bool IsPaused => IsReady && Status.Equals("Paused", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a user-friendly display label combining status and station name.
    /// </summary>
    public string DisplayLabel
    {
        get
        {
            if (!IsReady) return "AetherFM non disponibile";
            if (string.IsNullOrEmpty(StationName)) return Status;
            return $"{Status}: {StationName}";
        }
    }

    /// <summary>
    /// Validates the state data for consistency.
    /// </summary>
    /// <returns>True if the state is valid, false otherwise</returns>
    public bool IsValid
    {
        get
        {
            if (Volume01 < 0f || Volume01 > 1f) return false;
            if (LastUpdateUtc == DateTime.MinValue) return false;
            if (IsReady && string.IsNullOrEmpty(Status)) return false;
            return true;
        }
    }
}


