using System.Collections;
using UnityEngine;

/// A locker that holds the file. Walk up, press E, pass the lockpick minigame, and the file is
/// taken: the mission moves to the escape phase and the alarm goes off, exactly as if you had
/// walked over a plain file pickup.
///
/// Give it a solid collider on the Walls layer so it blocks movement and pathfinding.
/// You need one LockpickMinigame object in the scene.
public class LockedContainer : MonoBehaviour
{
    [SerializeField] LockpickSettings settings = new LockpickSettings();
    [SerializeField] float interactRange = 1.8f;
    [SerializeField] string promptText = "[E]  Pick lock";

    [Header("Optional visuals")]
    [Tooltip("Tinted when opened. Swap in an 'open' sprite later.")]
    [SerializeField] SpriteRenderer lockerSprite;
    [SerializeField] Color openedTint = new Color(0.55f, 0.8f, 0.9f);
    [Tooltip("Sprites played in order when the locker opens. One sprite = an instant swap to the open locker. The last one stays.")]
    [SerializeField] Sprite[] openingFrames;
    [SerializeField] float openingFps = 12f;

    public bool IsUnlocked { get; private set; }

    Transform player;
    PlayerController playerCtrl;
    Camera cam;
    bool inRange;
    GUIStyle promptStyle;

    void Awake()
    {
        cam = Camera.main;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            PlayerController pc = p.GetComponentInParent<PlayerController>();
            if (pc != null) { player = pc.transform; playerCtrl = pc; }
        }
        if (player == null)
            Debug.LogWarning("LockedContainer: no 'Player' tagged object with a PlayerController found.", this);
    }

    void Update()
    {
        inRange = false;

        if (IsUnlocked || player == null || Time.timeScale == 0f) return;
        if (playerCtrl != null && playerCtrl.InputLocked) return;   // hidden, or already busy

        GameManager gm = GameManager.Instance;
        if (gm != null && gm.State != MissionState.Infiltrate) return;   // only before the file is taken

        LockpickMinigame game = LockpickMinigame.Instance;
        if (game == null || game.IsActive) return;

        inRange = Vector2.Distance(player.position, transform.position) <= interactRange;

        if (inRange && Input.GetKeyDown(KeyCode.E))
            game.Begin(settings, OnPicked);
    }

    void OnPicked()
    {
        IsUnlocked = true;
        if (lockerSprite != null)
        {
            if (openingFrames != null && openingFrames.Length > 0) StartCoroutine(PlayOpening());
            else lockerSprite.color = openedTint;   // no art assigned: just tint it
        }

        if (GameManager.Instance != null)
            GameManager.Instance.CollectObjective(transform.position);   // takes the file, sets off the alarm
    }

    IEnumerator PlayOpening()
    {
        float step = 1f / Mathf.Max(1f, openingFps);
        foreach (Sprite frame in openingFrames)
        {
            lockerSprite.sprite = frame;
            yield return new WaitForSeconds(step);
        }
    }

    void OnGUI()
    {
        if (!inRange || cam == null) return;

        if (promptStyle == null)
            promptStyle = new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.MiddleCenter };

        Vector3 screen = cam.WorldToScreenPoint(transform.position + Vector3.up * 1.1f);
        GUI.Box(new Rect(screen.x - 80f, Screen.height - screen.y - 16f, 160f, 32f), promptText, promptStyle);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.35f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
