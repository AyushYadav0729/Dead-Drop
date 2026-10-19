using UnityEngine;
using UnityEngine.SceneManagement;

/// Polished, self-contained main menu. Uses Unity IMGUI so it needs no Canvas setup.
public class MainMenuController : MonoBehaviour
{
    [SerializeField] string firstLevelSceneName = "Level1";
    [SerializeField] bool useDebugGUI = true;

    bool showControls;
    GUIStyle titleStyle, subtitleStyle, buttonStyle, smallStyle, panelStyle, controlsStyle;
    Texture2D panelTexture, buttonTexture, buttonHoverTexture, accentTexture;
    bool stylesReady;

    void Awake()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    void Play()
    {
        if (!Application.CanStreamedLevelBeLoaded(firstLevelSceneName))
        {
            Debug.LogWarning($"MainMenu: scene '{firstLevelSceneName}' is not in Build Settings, or the name is wrong.");
            return;
        }

        RunStats.Reset();
        Time.timeScale = 1f;
        SceneManager.LoadScene(firstLevelSceneName);
    }

    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnGUI()
    {
        if (!useDebugGUI) return;
        EnsureStyles();

        // Darken the whole screen for a clean, game-like menu.
        Color old = GUI.color;
        GUI.color = new Color(0.025f, 0.04f, 0.065f, 0.94f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = old;

        // Small accent bars frame the panel.
        GUI.DrawTexture(new Rect(Screen.width * .5f - 220, Screen.height * .5f - 218, 440, 3), accentTexture);
        GUI.DrawTexture(new Rect(Screen.width * .5f - 220, Screen.height * .5f + 215, 440, 3), accentTexture);

        if (showControls) DrawControls();
        else DrawMain();
    }

    void DrawMain()
    {
        float w = Mathf.Clamp(Screen.width * .42f, 330f, 460f);
        float h = 430f;
        Rect r = new Rect((Screen.width - w) * .5f, (Screen.height - h) * .5f, w, h);
        GUI.Box(r, GUIContent.none, panelStyle);

        GUILayout.BeginArea(new Rect(r.x + 34, r.y + 28, r.width - 68, r.height - 55));
        GUILayout.Space(14);
        GUILayout.Label("DEAD DROP", titleStyle, GUILayout.Height(72));
        GUILayout.Label("INFILTRATE  •  SECURE  •  ESCAPE", subtitleStyle, GUILayout.Height(28));
        GUILayout.Space(34);

        if (GUILayout.Button("START MISSION", buttonStyle, GUILayout.Height(52))) Play();
        GUILayout.Space(12);
        if (GUILayout.Button("CONTROLS", buttonStyle, GUILayout.Height(46))) showControls = true;
        GUILayout.Space(12);
        if (GUILayout.Button("QUIT", buttonStyle, GUILayout.Height(46))) Quit();

        GUILayout.FlexibleSpace();
        GUILayout.Label("A STEALTH OPERATIONS PROTOTYPE", smallStyle, GUILayout.Height(24));
        GUILayout.EndArea();
    }

    void DrawControls()
    {
        // Keep the panel within the visible screen, including on short window sizes.
        float w = Mathf.Clamp(Screen.width * .54f, 340f, 560f);
        float h = Mathf.Clamp(Screen.height - 60f, 320f, 520f);
        Rect r = new Rect((Screen.width - w) * .5f, (Screen.height - h) * .5f, w, h);
        GUI.Box(r, GUIContent.none, panelStyle);

        float pad = 28f;
        float innerX = r.x + pad;
        float innerW = r.width - pad * 2f;
        float buttonH = 46f;
        float buttonY = r.y + r.height - pad - buttonH;

        GUI.Label(new Rect(innerX, r.y + 18f, innerW, 54f), "FIELD CONTROLS", titleStyle);
        string controls =
            "W A S D       Move\n" +
            "LEFT SHIFT   Run (louder footsteps)\n" +
            "SPACE          Dash\n" +
            "MOUSE          Aim\n" +
            "LEFT CLICK   Fire\n" +
            "RIGHT CLICK Laser sight\n" +
            "R                     Reload\n" +
            "1 / 2               Switch weapon\n" +
            "ESC                 Pause / Resume";
        float textY = r.y + 82f;
        float textH = Mathf.Max(100f, buttonY - textY - 12f);
        GUI.Label(new Rect(innerX, textY, innerW, textH), controls, controlsStyle);

        // Draw the Back button at a fixed position so it cannot be pushed off by GUILayout.
        if (GUI.Button(new Rect(innerX, buttonY, innerW, buttonH), "BACK TO MENU", buttonStyle))
            showControls = false;
    }

    void EnsureStyles()
    {
        if (stylesReady) return;
        panelTexture = MakeTexture(new Color(.035f, .055f, .08f, .98f));
        buttonTexture = MakeTexture(new Color(.08f, .12f, .16f, 1f));
        buttonHoverTexture = MakeTexture(new Color(.10f, .28f, .32f, 1f));
        accentTexture = MakeTexture(new Color(.15f, .88f, .72f, 1f));

        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = panelTexture;
        panelStyle.border = new RectOffset(2, 2, 2, 2);

        titleStyle = new GUIStyle(GUI.skin.label) {
            fontSize = Mathf.Clamp(Screen.width / 27, 32, 48),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = new Color(.93f, .98f, .98f);

        subtitleStyle = new GUIStyle(GUI.skin.label) {
            fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter
        };
        subtitleStyle.normal.textColor = new Color(.22f, .88f, .75f);

        buttonStyle = new GUIStyle(GUI.skin.button) {
            fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(12, 12, 8, 8)
        };
        buttonStyle.normal.background = buttonTexture;
        buttonStyle.hover.background = buttonHoverTexture;
        buttonStyle.active.background = buttonHoverTexture;
        buttonStyle.normal.textColor = Color.white;
        buttonStyle.hover.textColor = new Color(.35f, 1f, .84f);
        buttonStyle.active.textColor = Color.white;

        smallStyle = new GUIStyle(GUI.skin.label) {
            fontSize = 10, alignment = TextAnchor.MiddleCenter
        };
        smallStyle.normal.textColor = new Color(.42f, .52f, .57f);

        controlsStyle = new GUIStyle(GUI.skin.label) {
            fontSize = Mathf.Clamp(Screen.width / 95, 15, 19),
            alignment = TextAnchor.UpperLeft,
            wordWrap = true
        };
        controlsStyle.normal.textColor = new Color(.83f, .9f, .91f);
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
        if (panelTexture != null) Destroy(panelTexture);
        if (buttonTexture != null) Destroy(buttonTexture);
        if (buttonHoverTexture != null) Destroy(buttonHoverTexture);
        if (accentTexture != null) Destroy(accentTexture);
    }
}
