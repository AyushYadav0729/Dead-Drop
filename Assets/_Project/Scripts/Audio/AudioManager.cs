using UnityEngine;
using UnityEngine.SceneManagement;

/// Add this to one GameObject in MainMenu. It persists between scenes.
/// Import your own audio clips and assign them in the Inspector.
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource sfxSource;
    [SerializeField] AudioSource footstepSource;

    [Header("Music")]
    [SerializeField] AudioClip menuMusic;
    [SerializeField] AudioClip gameplayMusic;
    [Range(0f, 1f)] [SerializeField] float musicVolume = 0.35f;

    [Header("Weapons")]
    public AudioClip pistolShot;
    public AudioClip rifleShot;
    public AudioClip reload;
    public AudioClip emptyMagazine;

    [Header("Player")]
    public AudioClip footsteps;
    public AudioClip runningFootsteps;
    public AudioClip dash;
    public AudioClip playerHurt;
    public AudioClip playerDeath;
    public AudioClip healthPickup;
    public AudioClip ammoPickup;

    [Header("World / Mission")]
    public AudioClip alarm;
    public AudioClip door;
    public AudioClip objective;
    public AudioClip missionComplete;
    public AudioClip missionFailed;
    public AudioClip uiClick;

    [Range(0f, 1f)] [SerializeField] float sfxVolume = 0.8f;

    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
            if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        }

        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        if (footstepSource == null) footstepSource = gameObject.AddComponent<AudioSource>();

        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = musicVolume;
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        footstepSource.playOnAwake = false;
        footstepSource.loop = false;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start() => PlayMusicForScene(SceneManager.GetActiveScene().name);

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => PlayMusicForScene(scene.name);

    void PlayMusicForScene(string sceneName)
    {
        AudioClip wanted = sceneName == "MainMenu" ? menuMusic : gameplayMusic;
        if (wanted == null)
        {
            if (musicSource.isPlaying) musicSource.Stop();
            musicSource.clip = null;
            return;
        }

        if (musicSource.clip == wanted && musicSource.isPlaying) return;
        musicSource.clip = wanted;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(sfxVolume * volumeScale));
    }

    // Each footstep is a one-shot. StopFootsteps() immediately cuts it when movement ends.
    public void PlayFootstep(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || footstepSource == null) return;
        if (footstepSource.isPlaying) return;
        footstepSource.clip = clip;
        footstepSource.volume = Mathf.Clamp01(sfxVolume * volumeScale);
        footstepSource.loop = false;
        footstepSource.Play();
    }

    public void StopFootsteps()
    {
        if (footstepSource == null) return;
        footstepSource.Stop();
        footstepSource.clip = null;
    }

    public void PlayWeaponShot(Weapon weapon)
    {
        if (weapon == null) return;
        string weaponName = weapon.Data != null ? weapon.Data.name.ToLowerInvariant() : weapon.name.ToLowerInvariant();
        AudioClip clip = weaponName.Contains("rifle") ? rifleShot : pistolShot;
        PlaySfx(clip);
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        if (musicSource != null) musicSource.volume = musicVolume;
        PlayerPrefs.SetFloat("DeadDrop_MusicVolume", musicVolume);
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("DeadDrop_SfxVolume", sfxVolume);
    }
}
