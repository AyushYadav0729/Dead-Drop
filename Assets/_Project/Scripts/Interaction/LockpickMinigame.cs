using System;
using UnityEngine;

/// Difficulty for one lock. Each locker has its own, so Level 1 can be gentle and Level 2 harsh.
[Serializable]
public class LockpickSettings
{
    [Range(1, 6)] public int gates = 3;
    [Tooltip("Height of each gap as a fraction of the channel. Smaller = harder.")]
    [Range(0.18f, 0.6f)] public float gapHeight = 0.32f;
    [Tooltip("How fast the gaps slide up and down, in full cycles per second.")]
    [Range(0.15f, 1.2f)] public float gapSpeed = 0.4f;
}

/// The lockpick minigame. One per scene (put it on an empty object).
///
/// A horizontal pick must travel left to right through vertical gates. Each gate has a gap
/// that slides up and down. Move the pick with W/S, push it forward by holding D or Space.
/// If the pick's head touches a gate the pick jams: the attempt resets and makes a little noise
/// that nearby guards can hear.
///
/// It runs in REAL TIME. Guards keep patrolling, and the player can cancel at any moment with Q.
/// The player cannot move or shoot while picking. Being shot or dying cancels the attempt.
///
/// All the logic is in a normalised space (x and y from 0 to 1) so it does not depend on the
/// screen size. OnGUI just draws it. The drawing is a placeholder that can be replaced with real UI.
public class LockpickMinigame : MonoBehaviour
{
    public static LockpickMinigame Instance { get; private set; }

    [Header("Pick feel")]
    [SerializeField] float rodSpeed = 1.2f;        // vertical, channel-heights per second
    [SerializeField] float pushSpeed = 0.38f;      // forward, channel-widths per second
    [SerializeField] float headLength = 0.045f;
    [SerializeField] float headHeight = 0.12f;
    [SerializeField] float barThickness = 0.03f;

    [Header("Failure")]
    [Tooltip("How far a jam is heard by guards.")]
    [SerializeField] float failNoiseRadius = 6f;
    [SerializeField] float failLockoutSeconds = 0.7f;
    [SerializeField] float successSeconds = 0.6f;

    public bool IsActive { get; private set; }

    enum Phase { Picking, Failed, Success }

    Phase phase;
    Action onSuccess;
    PlayerController playerController;
    Health playerHealth;

    int gateCount;
    float gapHeight;
    float gapHalf;
    float gapAmplitude;
    float[] gateX, gateSpeed, gatePhase;

    float time;
    float phaseEnd;
    float tip;     // x position of the front of the pick, 0..1
    float rodY;    // 0 = bottom of the channel, 1 = top
    int failCount;

    static readonly float[] SpeedVariety = { 1.0f, 1.25f, 0.85f, 1.15f, 0.95f, 1.3f };

    GUIStyle titleStyle, helpStyle, bigStyle;

    float HeadHalf => headHeight * 0.5f;

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        ReleasePlayer();
    }

    // ------------------------------------------------------------ starting and stopping

    public void Begin(LockpickSettings settings, Action onSuccessCallback)
    {
        if (IsActive) return;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return;
        playerController = p.GetComponentInParent<PlayerController>();
        playerHealth = p.GetComponentInParent<Health>();
        if (playerController == null || playerHealth == null) return;

        onSuccess = onSuccessCallback;

        gateCount = Mathf.Clamp(settings.gates, 1, 6);
        gapHeight = settings.gapHeight;
        gapHalf = gapHeight * 0.5f;
        gapAmplitude = (1f - gapHeight) * 0.5f;   // keeps the gap inside the channel

        gateX = new float[gateCount];
        gateSpeed = new float[gateCount];
        gatePhase = new float[gateCount];
        for (int i = 0; i < gateCount; i++)
        {
            gateX[i] = gateCount == 1 ? 0.5f : Mathf.Lerp(0.22f, 0.82f, i / (float)(gateCount - 1));
            gateSpeed[i] = settings.gapSpeed * SpeedVariety[i % SpeedVariety.Length];
            gatePhase[i] = Mathf.Repeat(i * 0.37f + 0.1f, 1f);
        }

        time = 0f;
        failCount = 0;
        ResetPick();
        phase = Phase.Picking;

        playerController.InputLocked = true;           // no moving or shooting while picking
        playerHealth.Damaged += OnPlayerDamaged;
        playerHealth.Died += OnPlayerDied;

        IsActive = true;
    }

    void End(bool success)
    {
        IsActive = false;
        ReleasePlayer();

        if (success) onSuccess?.Invoke();
        onSuccess = null;
    }

    void ReleasePlayer()
    {
        if (playerController != null) playerController.InputLocked = false;
        if (playerHealth != null)
        {
            playerHealth.Damaged -= OnPlayerDamaged;
            playerHealth.Died -= OnPlayerDied;
        }
    }

    void OnPlayerDamaged(float amount, Vector2 from) => End(false);
    void OnPlayerDied() => End(false);

    void ResetPick()
    {
        tip = headLength;
        rodY = 0.5f;
    }

    // ------------------------------------------------------------ logic

    void Update()
    {
        if (!IsActive || Time.timeScale == 0f) return;

        if (Input.GetKeyDown(KeyCode.Q)) { End(false); return; }

        float dt = Time.deltaTime;
        time += dt;

        if (phase == Phase.Failed)
        {
            if (time >= phaseEnd) { ResetPick(); phase = Phase.Picking; }
            return;
        }
        if (phase == Phase.Success)
        {
            if (time >= phaseEnd) End(true);
            return;
        }

        // Move the pick up and down.
        float v = 0f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v -= 1f;
        rodY = Mathf.Clamp(rodY + v * rodSpeed * dt, HeadHalf, 1f - HeadHalf);

        // Push it forward. Letting go just holds it where it is.
        bool pushing = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.Space);
        if (pushing) tip = Mathf.Min(1f, tip + pushSpeed * dt);

        if (HitsAGate()) { Fail(); return; }

        if (tip >= 1f)
        {
            phase = Phase.Success;
            phaseEnd = time + successSeconds;
        }
    }

    float GapCenter(int i) =>
        0.5f + gapAmplitude * Mathf.Sin(2f * Mathf.PI * (time * gateSpeed[i] + gatePhase[i]));

    /// Only the pick's HEAD can collide. The shaft behind it is a thin wire that slips through.
    /// (If the whole rod collided, passing the second gate would force it to fit through every
    /// earlier gap at once, which no moving gaps could ever allow.)
    bool HitsAGate()
    {
        float headFront = tip;
        float headBack = tip - headLength;
        float headTop = rodY + HeadHalf;
        float headBottom = rodY - HeadHalf;

        for (int i = 0; i < gateCount; i++)
        {
            float barLeft = gateX[i] - barThickness * 0.5f;
            float barRight = gateX[i] + barThickness * 0.5f;
            if (headFront < barLeft || headBack > barRight) continue; // not inside this gate

            float c = GapCenter(i);
            bool insideGap = headTop <= c + gapHalf && headBottom >= c - gapHalf;
            if (!insideGap) return true;
        }
        return false;
    }

    void Fail()
    {
        phase = Phase.Failed;
        phaseEnd = time + failLockoutSeconds;
        failCount++;

        // A jammed pick rattles. Guards within range go and investigate.
        if (playerController != null)
            NoiseSystem.Emit(playerController.transform.position, failNoiseRadius, Team.Player);
    }

    // ------------------------------------------------------------ placeholder drawing

    void OnGUI()
    {
        if (!IsActive || Event.current.type != EventType.Repaint) return;

        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            helpStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, alignment = TextAnchor.MiddleCenter };
            bigStyle = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        }

        float w = Mathf.Min(Screen.width * 0.72f, 920f);
        float h = w * 0.46f;
        Rect panel = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);

        if (phase == Phase.Failed)   // shake on a jam
        {
            panel.x += UnityEngine.Random.Range(-6f, 6f);
            panel.y += UnityEngine.Random.Range(-3f, 3f);
        }

        Fill(panel, new Color(0.06f, 0.08f, 0.12f, 0.97f));
        Frame(panel, 2f, new Color(0.24f, 0.84f, 0.96f));
        GUI.Label(new Rect(panel.x, panel.y + 10f, panel.width, 36f), "LOCKPICK", titleStyle);

        Rect ch = new Rect(panel.x + 50f, panel.y + 70f, panel.width - 100f, panel.height - 150f);
        Fill(ch, new Color(0.03f, 0.04f, 0.07f));
        Frame(ch, 2f, new Color(0.35f, 0.4f, 0.5f));

        // The lock core at the far end: this is the goal.
        Fill(new Rect(ch.xMax - 10f, ch.y, 10f, ch.height), new Color(1f, 0.85f, 0.35f));

        // Gates.
        float barPx = barThickness * ch.width;
        for (int i = 0; i < gateCount; i++)
        {
            float bx = ch.x + gateX[i] * ch.width - barPx * 0.5f;
            float c = GapCenter(i);
            float gapTopPx = ch.y + (1f - (c + gapHalf)) * ch.height;
            float gapBottomPx = ch.y + (1f - (c - gapHalf)) * ch.height;

            Color bar = new Color(0.55f, 0.6f, 0.7f);
            Fill(new Rect(bx, ch.y, barPx, gapTopPx - ch.y), bar);
            Fill(new Rect(bx, gapBottomPx, barPx, ch.yMax - gapBottomPx), bar);
        }

        // The pick: a thin shaft and a thicker head.
        Color rod = phase == Phase.Failed ? new Color(1f, 0.25f, 0.2f)
                  : phase == Phase.Success ? new Color(0.4f, 1f, 0.55f)
                  : new Color(0.24f, 0.84f, 0.96f);

        float shaftEnd = ch.x + (tip - headLength) * ch.width;
        float ry = ch.y + (1f - rodY) * ch.height;
        Fill(new Rect(ch.x, ry - 2f, Mathf.Max(0f, shaftEnd - ch.x), 4f), rod);
        Fill(new Rect(shaftEnd, ry - HeadHalf * ch.height, headLength * ch.width, headHeight * ch.height), rod);

        GUI.Label(new Rect(panel.x, panel.yMax - 62f, panel.width, 24f),
            "W / S  move the pick      hold D or Space  push      Q  cancel", helpStyle);
        GUI.Label(new Rect(panel.x, panel.yMax - 38f, panel.width, 24f),
            failCount == 0 ? "Don't touch the gates. A jam makes noise." : $"Jams: {failCount}   (each one makes noise)", helpStyle);

        if (phase == Phase.Failed) GUI.Label(ch, "JAMMED", bigStyle);
        if (phase == Phase.Success) GUI.Label(ch, "UNLOCKED", bigStyle);
    }

    static void Fill(Rect r, Color c)
    {
        Color old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }

    static void Frame(Rect r, float t, Color c)
    {
        Fill(new Rect(r.x, r.y, r.width, t), c);
        Fill(new Rect(r.x, r.yMax - t, r.width, t), c);
        Fill(new Rect(r.x, r.y, t, r.height), c);
        Fill(new Rect(r.xMax - t, r.y, t, r.height), c);
    }
}
