using System;
using UnityEngine;

/// "I see the player over there!" An enemy that spots the player broadcasts it; nearby
/// allies (within the radius of the CALLER) go and search that spot. They are not told where
/// the player goes afterwards, only where he was.
public static class SquadCall
{
    /// (caller position, where the player was seen, call radius, the caller)
    public static event Action<Vector2, Vector2, float, GameObject> Called;

    public static void Make(Vector2 callerPosition, Vector2 target, float radius, GameObject caller)
        => Called?.Invoke(callerPosition, target, radius, caller);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Called = null;
}
