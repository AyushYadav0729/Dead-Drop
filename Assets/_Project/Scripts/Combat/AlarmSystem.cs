using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// Global alarm flag. GameManager raises it when the player takes the file.
/// Origin is where it was raised: enemies converge there first, not on the player.
/// Resets automatically whenever a scene loads, so "Restart Level" starts calm.
public static class AlarmSystem
{
    public static bool Active { get; private set; }
    public static Vector2 Origin { get; private set; }
    public static event Action Raised;

    public static void Raise(Vector2 origin)
    {
        if (Active) return;
        Active = true;
        Origin = origin;
        Raised?.Invoke();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        Active = false;
        Raised = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Active = false;
}
