using System.Collections.Generic;
using UnityEngine;

/// Remembers the score of each level across scene loads so the final screen can show a run total.
/// Keyed by scene index, so replaying a level OVERWRITES its score instead of adding to it.
public static class RunStats
{
    static readonly Dictionary<int, int> levelScores = new Dictionary<int, int>();

    public static int Total
    {
        get
        {
            int total = 0;
            foreach (KeyValuePair<int, int> kv in levelScores) total += kv.Value;
            return total;
        }
    }

    public static void SetLevelScore(int levelIndex, int score) => levelScores[levelIndex] = score;

    public static void Reset() => levelScores.Clear();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init() => levelScores.Clear();
}
