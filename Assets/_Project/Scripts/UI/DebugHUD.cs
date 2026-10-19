using System;
using UnityEngine;

/// Keeps mission feedback and pause/end menus, styled to match the Dead Drop main menu.
/// Health and ammo are intentionally left to CanvasHUD to avoid duplicate UI.
public class DebugHUD : MonoBehaviour
{
    [SerializeField] PlayerController player;
    [SerializeField] Health playerHealth;

    GUIStyle title, body, small, panelStyle, buttonStyle;
    Texture2D overlayTexture, panelTexture, buttonTexture, buttonHoverTexture, accentTexture;
    bool stylesReady;

    void Update()
    {
        // TEST ONLY: take the file from anywhere.
        if (Input.GetKeyDown(KeyCode.K) && GameManager.Instance != null && playerHealth != null)
            GameManager.Instance.CollectObjective(playerHealth.transform.position);
    }

    void OnGUI()
    {
        EnsureStyles();

        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        bool showPanel = gm.Paused || gm.State == MissionState.Complete || gm.State == MissionState.Failed;
        if (showPanel)
        {
            Color old = GUI.color;
            GUI.color = new Color(0.015f, 0.025f, 0.04f, 0.82f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), overlayTexture);
            GUI.color = old;
        }

        // Compact mission information remains visible while playing.
        if (!showPanel)
        {
            GUI.Label(new Rect(18, 18, 400, 28), $"SCORE  {gm.RunningScore}     KILLS  {gm.Kills}", small);
            GUI.Label(new Rect(18, 42, 400, 24), $"TIME  {FormatTime(gm.Elapsed)}", small);
            if (player != null && player.CurrentWeapon != null && player.CurrentWeapon.Data != null)
                GUI.Label(new Rect(18, 96, 400, 24), $"WEAPON  {player.CurrentWeapon.Data.displayName}", small);
            GUI.Label(new Rect(Screen.width / 2f - 260, 14, 520, 30), gm.ObjectiveText, body);

            if (AlarmSystem.Active)
                GUI.Label(new Rect(Screen.width / 2f - 200, 44, 400, 32), "!! ALARM ACTIVE !!", title);

            if (!string.IsNullOrEmpty(gm.HintText))
                GUI.Label(new Rect(Screen.width / 2f - 240, 78, 480, 30), gm.HintText, body);

            if (player != null)
            {
                string move = player.IsDashing ? "DASH" : (player.IsRunning ? "RUN • NOISY" : "WALK");
                string dash = player.DashReady ? "READY" : $"{player.DashCooldownRemaining:0.0}s";
                GUI.Label(new Rect(18, 72, 400, 24),
                    $"{move}     DASH {dash}{(player.IsHidden ? "     HIDDEN" : "")}", small);
            }
        }

        if (gm.State == MissionState.Failed)
        {
            DrawPanel("MISSION FAILED", "You've been taken out. Regroup and try again.",
                ("RETRY MISSION", gm.RestartLevel),
                ("MAIN MENU", gm.LoadMainMenu));
        }
        else if (gm.State == MissionState.Complete)
        {
            ScoreBreakdown r = gm.Result;
            string resultText =
                $"TIME      {FormatTime(gm.Elapsed)}  /  PAR {FormatTime(gm.ParTime)}\n" +
                $"COMPLETION BONUS      +{r.completion}\n" +
                $"TIME BONUS                   +{r.timeBonus}\n" +
                $"KILLS ({gm.Kills})                 +{r.kills}\n" +
                $"STEALTH BONUS            +{r.stealth}\n" +
                $"GHOST ESCAPE BONUS   +{r.ghost}\n" +
                $"TOTAL SCORE               {r.Total}\n" +
                $"RUN TOTAL                   {gm.RunTotal}";

            if (gm.HasNextLevel)
            {
                DrawPanel("MISSION COMPLETE", resultText,
                    ("NEXT LEVEL", gm.NextLevel),
                    ("RETRY LEVEL", gm.RestartLevel),
                    ("MAIN MENU", gm.LoadMainMenu));
            }
            else
            {
                DrawPanel("THANKS FOR PLAYING", "OPERATION COMPLETE\n\n" + resultText,
                    ("PLAY AGAIN", gm.RestartLevel),
                    ("MAIN MENU", gm.LoadMainMenu));
            }
        }
        else if (gm.Paused)
        {
            DrawPanel("MISSION PAUSED", "Take a breath. The operation can wait.",
                ("RESUME", () => gm.SetPaused(false)),
                ("RESTART LEVEL", gm.RestartLevel),
                ("MAIN MENU", gm.LoadMainMenu));
        }
    }

    void DrawPanel(string heading, string message, params (string label, Action onClick)[] buttons)
    {
        float w = Mathf.Min(520f, Screen.width - 32f);
        int lines = string.IsNullOrEmpty(message) ? 0 : message.Split('\n').Length;
        float h = Mathf.Min(Screen.height - 32f, 112f + lines * 23f + buttons.Length * 50f);
        Rect r = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);

        GUI.DrawTexture(new Rect(r.x, r.y, r.width, 3f), accentTexture);
        GUI.Box(r, GUIContent.none, panelStyle);

        GUILayout.BeginArea(new Rect(r.x + 24f, r.y + 14f, r.width - 48f, r.height - 28f));
        GUILayout.Label(heading, title, GUILayout.Height(heading.Length > 18 ? 54f : 62f));

        if (!string.IsNullOrEmpty(message))
        {
            GUILayout.Space(4);
            GUILayout.Label(message, body);
        }

        GUILayout.Space(10);
        foreach (var b in buttons)
        {
            if (GUILayout.Button(b.label, buttonStyle, GUILayout.Height(42f)))
                b.onClick();
            GUILayout.Space(6);
        }
        GUILayout.EndArea();
    }

    void EnsureStyles()
    {
        if (stylesReady) return;

        overlayTexture = MakeTexture(Color.white);
        panelTexture = MakeTexture(new Color(0.025f, 0.045f, 0.065f, 0.99f));
        buttonTexture = MakeTexture(new Color(0.07f, 0.11f, 0.15f, 1f));
        buttonHoverTexture = MakeTexture(new Color(0.08f, 0.25f, 0.27f, 1f));
        accentTexture = MakeTexture(new Color(0.16f, 0.9f, 0.72f, 1f));

        title = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Clamp(Screen.width / 32, 25, 38),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        title.normal.textColor = new Color(0.92f, 0.98f, 0.98f);

        body = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Clamp(Screen.width / 100, 13, 17),
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            richText = true
        };
        body.normal.textColor = new Color(0.72f, 0.82f, 0.84f);

        small = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        small.normal.textColor = new Color(0.5f, 0.92f, 0.82f);

        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = panelTexture;
        panelStyle.border = new RectOffset(2, 2, 2, 2);

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(12, 12, 8, 8)
        };
        buttonStyle.normal.background = buttonTexture;
        buttonStyle.hover.background = buttonHoverTexture;
        buttonStyle.active.background = buttonHoverTexture;
        buttonStyle.normal.textColor = Color.white;
        buttonStyle.hover.textColor = new Color(0.35f, 1f, 0.84f);
        buttonStyle.active.textColor = Color.white;

        stylesReady = true;
    }

    static Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    void OnDestroy()
    {
        if (overlayTexture != null) Destroy(overlayTexture);
        if (panelTexture != null) Destroy(panelTexture);
        if (buttonTexture != null) Destroy(buttonTexture);
        if (buttonHoverTexture != null) Destroy(buttonHoverTexture);
        if (accentTexture != null) Destroy(accentTexture);
    }

    static string FormatTime(float seconds) => $"{(int)(seconds / 60):00}:{(int)(seconds % 60):00}";
}
