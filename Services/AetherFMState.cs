using System;

namespace Umbra.AetherFM.Services;

public readonly record struct AetherFMState(
    bool IsReady,
    string Status,
    string StationName,
    string StationUrl,
    float Volume01,
    DateTime LastUpdateUtc
)
{
    public static AetherFMState Unavailable(DateTime nowUtc) =>
        new(false, string.Empty, string.Empty, string.Empty, 0f, nowUtc);
}


