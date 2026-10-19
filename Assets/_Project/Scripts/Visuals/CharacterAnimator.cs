using System;
using UnityEngine;

/// One animation: a list of frames played at a frame rate.
[Serializable]
public class SpriteClip
{
    public Sprite[] frames;
    public float fps = 10f;
    public bool loop = true;

    public bool HasFrames => frames != null && frames.Length > 0;
}

/// Simple frame-by-frame animation for the player and for enemies. Put it on the SAME object as the
/// SpriteRenderer (the "Visual" child), next to FacingSprite. Every clip is optional: leave a clip
/// empty and the animator just falls back to the next best one.
///
/// It chooses the clip by itself:
///   Death    the character is dead
///   Dash     the player is dashing
///   Shoot    just fired a shot while standing still
///   Run      moving fast (the player holding Shift, or an enemy that is chasing)
///   Walk     moving
///   Idle     standing still
///
/// To fill a clip: select all its frames in the Project window and drag them onto the "Frames"
/// label in the Inspector. Give every frame the same pivot (the centre of the body).
[RequireComponent(typeof(SpriteRenderer))]
public class CharacterAnimator : MonoBehaviour
{
    [Header("Clips (leave any empty to skip it)")]
    [SerializeField] SpriteClip idle = new SpriteClip { fps = 6f };
    [SerializeField] SpriteClip walk = new SpriteClip { fps = 9f };
    [SerializeField] SpriteClip run = new SpriteClip { fps = 13f };
    [SerializeField] SpriteClip dash = new SpriteClip { fps = 16f, loop = false };
    [SerializeField] SpriteClip shoot = new SpriteClip { fps = 14f, loop = false };
    [SerializeField] SpriteClip death = new SpriteClip { fps = 10f, loop = false };

    [Header("Tuning")]
    [Tooltip("Below this speed the character counts as standing still.")]
    [SerializeField] float movingThreshold = 0.15f;
    [Tooltip("How long the shoot pose is held after each shot.")]
    [SerializeField] float shootHoldSeconds = 0.25f;

    SpriteRenderer sr;
    Rigidbody2D rb;
    Health health;
    PlayerController player;
    EnemyController enemy;
    Weapon[] weapons;

    SpriteClip current;
    float clipTime;
    float shootUntil;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponentInParent<Rigidbody2D>();
        health = GetComponentInParent<Health>();
        player = GetComponentInParent<PlayerController>();
        enemy = GetComponentInParent<EnemyController>();

        Transform root = health != null ? health.transform : transform.root;
        weapons = root.GetComponentsInChildren<Weapon>(true);
    }

    void OnEnable()
    {
        if (weapons == null) return;
        foreach (Weapon w in weapons) if (w != null) w.Fired += OnFired;
    }

    void OnDisable()
    {
        if (weapons == null) return;
        foreach (Weapon w in weapons) if (w != null) w.Fired -= OnFired;
    }

    void OnFired()
    {
        shootUntil = Time.time + shootHoldSeconds;
        if (current == shoot) clipTime = 0f;   // restart the shoot animation on every shot
    }

    void Update()
    {
        SpriteClip wanted = Choose();
        if (wanted != current)
        {
            current = wanted;
            clipTime = 0f;
        }
        Play();
    }

    SpriteClip Choose()
    {
        if (health != null && health.IsDead && death.HasFrames) return death;

        if (player != null && player.IsDashing && dash.HasFrames) return dash;

        bool moving = Speed() > movingThreshold;

        if (!moving && Time.time < shootUntil && shoot.HasFrames) return shoot;

        if (moving)
        {
            bool running = player != null
                ? player.IsRunning
                : enemy != null && enemy.State == EnemyState.Chase;

            if (running && run.HasFrames) return run;
            if (walk.HasFrames) return walk;
            if (run.HasFrames) return run;
        }

        if (idle.HasFrames) return idle;
        return current;   // nothing suitable: keep whatever was showing
    }

    void Play()
    {
        if (current == null || !current.HasFrames) return;

        clipTime += Time.deltaTime;
        int count = current.frames.Length;
        int index = (int)(clipTime * current.fps);
        index = current.loop ? index % count : Mathf.Min(index, count - 1);

        sr.sprite = current.frames[index];
    }

    float Speed()
    {
        if (rb == null) return 0f;
#if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity.magnitude;
#else
        return rb.velocity.magnitude;
#endif
    }
}
