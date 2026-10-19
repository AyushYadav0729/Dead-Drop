using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum MissionState { Infiltrate, Escape, Complete, Failed }

[Serializable]
public struct ScoreBreakdown
{
    public int completion, timeBonus, kills, stealth, ghost;
    public int Total => completion + timeBonus + kills + stealth + ghost;
}

/// One per level scene. Owns the mission state, timer, kills, stealth tracking and scoring.
///
///   Infiltrate --(file taken: alarm raised)--> Escape --(reach exit)--> Complete
///        \_______________ player dies (any time) ______________________> Failed
///
/// Two separate stealth bonuses:
///   Stealth      file taken before any enemy ever chased the player.
///   Ghost Escape no enemy chased the player between the alarm and the exit.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Level")]
    [SerializeField] string levelTitle = "Level 1";
    [Tooltip("Scene to load after this one. Leave empty on the last level.")]
    [SerializeField] string nextSceneName = "";
    [SerializeField] string mainMenuSceneName = "MainMenu";

    [Header("Scoring")]
    [SerializeField] int completionPoints = 1000;
    [SerializeField] float parTimeSeconds = 120f;
    [SerializeField] int pointsPerSecondUnderPar = 10;
    [SerializeField] int pointsPerKill = 100;
    [SerializeField] int stealthBonus = 500;
    [SerializeField] int ghostEscapeBonus = 500;

    public MissionState State { get; private set; }
    public float Elapsed { get; private set; }
    public int Kills { get; private set; }
    public bool StealthIntact { get; private set; } = true;
    public bool GhostEscapeIntact { get; private set; } = true;
    public bool Paused { get; private set; }
    public ScoreBreakdown Result { get; private set; }

    public string LevelTitle => levelTitle;
    public float ParTime => parTimeSeconds;
    public int RunningScore => Kills * pointsPerKill;
    public int RunTotal => RunStats.Total;
    public bool IsFinalLevel => !HasNextLevel;
    public bool HasNextLevel => !string.IsNullOrEmpty(nextSceneName) && Application.CanStreamedLevelBeLoaded(nextSceneName);
    public string HintText => Time.unscaledTime < hintUntil ? hint : "";

    public string ObjectiveText
    {
        get
        {
            switch (State)
            {
                case MissionState.Infiltrate: return "Objective: steal the file";
                case MissionState.Escape: return "ALARM! Reach the exit";
                case MissionState.Complete: return "Mission complete";
                default: return "Mission failed";
            }
        }
    }

    public event Action<MissionState> StateChanged;

    readonly List<EnemyController> enemies = new List<EnemyController>();
    Health playerHealth;
    string hint;
    float hintUntil;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f; // a previous level may have left the game frozen
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (playerHealth != null) playerHealth.Died -= Fail;
        Time.timeScale = 1f;
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerHealth = p.GetComponentInParent<Health>();
        if (playerHealth != null) playerHealth.Died += Fail;
        else Debug.LogWarning("GameManager: no 'Player' tagged object with Health found.", this);

        foreach (EnemyController enemy in FindObjectsByType<EnemyController>())
        {
            RegisterEnemy(enemy);
        }
    }

    void Update()
    {
        bool playing = State == MissionState.Infiltrate || State == MissionState.Escape;

        if (playing && !Paused) Elapsed += Time.deltaTime;
        if (playing && Input.GetKeyDown(KeyCode.Escape)) SetPaused(!Paused);
    }

    // ------------------------------------------------------------ called by other scripts

    /// alarmOrigin = where the file was taken. Enemies converge there first.
    public void CollectObjective(Vector2 alarmOrigin)
    {
        if (State != MissionState.Infiltrate) return;

        // If someone is already chasing the player at this moment, the ghost escape is already lost.
        GhostEscapeIntact = !AnyEnemyChasing();

        State = MissionState.Escape;
        StateChanged?.Invoke(State);
        AlarmSystem.Raise(alarmOrigin);
    }

    public void TryExit()
    {
        if (State == MissionState.Escape) Complete();
        else if (State == MissionState.Infiltrate) ShowHint("Steal the file first.");
    }

    /// For security cameras and tripwires to call when they spot the player.
    public void ReportSpotted()
    {
        if (State == MissionState.Infiltrate) StealthIntact = false;
        else if (State == MissionState.Escape) GhostEscapeIntact = false;
    }

    /// Enemies created after the level starts (reinforcements) call this so that kills and
    /// stealth tracking include them.
    public void RegisterEnemy(EnemyController enemy)
    {
        if (enemy == null || enemies.Contains(enemy)) return;
        enemies.Add(enemy);
        enemy.StateChanged += OnEnemyStateChanged;
        enemy.GetComponent<Health>().Died += OnEnemyDied;
    }

    // ------------------------------------------------------------ outcomes

    void Complete()
    {
        if (State != MissionState.Escape) return;

        State = MissionState.Complete;
        Result = new ScoreBreakdown
        {
            completion = completionPoints,
            timeBonus = Mathf.RoundToInt(Mathf.Max(0f, parTimeSeconds - Elapsed) * pointsPerSecondUnderPar),
            kills = Kills * pointsPerKill,
            stealth = StealthIntact ? stealthBonus : 0,
            ghost = GhostEscapeIntact ? ghostEscapeBonus : 0
        };

        RunStats.SetLevelScore(SceneManager.GetActiveScene().buildIndex, Result.Total);

        StateChanged?.Invoke(State);
        StartCoroutine(FreezeAfter(0.4f));
    }

    void Fail()
    {
        if (State == MissionState.Complete || State == MissionState.Failed) return;

        State = MissionState.Failed;
        StateChanged?.Invoke(State);
        StartCoroutine(FreezeAfter(1f)); // short delay so the death is visible
    }

    IEnumerator FreezeAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Time.timeScale = 0f;
    }

    // ------------------------------------------------------------ enemy tracking

    bool AnyEnemyChasing()
    {
        foreach (EnemyController e in enemies)
            if (e != null && e.enabled && e.State == EnemyState.Chase) return true; // disabled = dead
        return false;
    }

    void OnEnemyStateChanged(EnemyState previous, EnemyState next)
    {
        if (next != EnemyState.Chase) return;

        // A chase before the alarm breaks Stealth; a chase after it breaks Ghost Escape.
        if (!AlarmSystem.Active) ReportSpotted();
        else if (State == MissionState.Escape) GhostEscapeIntact = false;
    }

    void OnEnemyDied()
    {
        if (State == MissionState.Infiltrate || State == MissionState.Escape) Kills++;
    }

    // ------------------------------------------------------------ pause / scene flow

    public void SetPaused(bool paused)
    {
        Paused = paused;
        Time.timeScale = paused ? 0f : 1f;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void NextLevel()
    {
        if (!HasNextLevel) return;
        Time.timeScale = 1f;
        SceneManager.LoadScene(nextSceneName);
    }

    public void LoadMainMenu()
    {
        if (!Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
        {
            Debug.LogWarning($"GameManager: scene '{mainMenuSceneName}' is not in Build Settings yet.");
            return;
        }
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void ShowHint(string text)
    {
        hint = text;
        hintUntil = Time.unscaledTime + 2f;
    }
}
