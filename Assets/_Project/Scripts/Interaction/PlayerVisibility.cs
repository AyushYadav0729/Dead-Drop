using System.Collections.Generic;
using UnityEngine;

/// Which watchers (guards and cameras) can see the player RIGHT NOW. Each watcher reports
/// every frame. HidingSpot uses it to refuse hiding while someone is looking at you.
public static class PlayerVisibility
{
    static readonly HashSet<Component> watchers = new HashSet<Component>();

    public static bool IsSeen => watchers.Count > 0;

    public static void Set(Component watcher, bool seesPlayer)
    {
        if (seesPlayer) watchers.Add(watcher);
        else watchers.Remove(watcher);
    }

    public static void Clear(Component watcher) => watchers.Remove(watcher);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init() => watchers.Clear();
}
