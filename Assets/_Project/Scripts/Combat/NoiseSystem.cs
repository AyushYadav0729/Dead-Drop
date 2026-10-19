using System;
using UnityEngine;

/// Global "something loud happened" channel. Weapons emit noise when they fire;
/// enemies will subscribe to NoiseMade (Day 2) to investigate gunshots.
public static class NoiseSystem
{
    /// (position, radius, team that made the noise)
    public static event Action<Vector2, float, Team> NoiseMade;

    public static void Emit(Vector2 position, float radius, Team source)
        => NoiseMade?.Invoke(position, radius, source);

    // Clears subscribers when entering play mode with domain reload disabled.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => NoiseMade = null;
}
