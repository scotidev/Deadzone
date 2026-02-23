using UnityEngine;

/// <summary>
/// Persistent manager responsible for all audio in the game.
/// Lives in the Loader scene and survives all scene changes via DontDestroyOnLoad.
/// Requires two AudioSource components assigned in the inspector: one for BGM, one for SFX.
/// </summary>
public class AudioManager : MonoBehaviour {
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Default Volumes")]
    [Range(0f, 1f)] [SerializeField] private float bgmVolume = 0.5f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;

    public float BGMVolume => bgmVolume;
    public float SFXVolume => sfxVolume;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeVolumes();
        } else {
            Destroy(gameObject);
        }
    }

    private void InitializeVolumes() {
        if (bgmSource != null) bgmSource.volume = bgmVolume;
        if (sfxSource != null) sfxSource.volume = sfxVolume;
    }

    /// <summary>
    /// Plays a background music clip. Loops by default.
    /// </summary>
    public void PlayBGM(AudioClip clip, bool loop = true) {
        if (bgmSource == null || clip == null) return;
        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.Play();
    }

    /// <summary>
    /// Stops the currently playing background music.
    /// </summary>
    public void StopBGM() {
        bgmSource?.Stop();
    }

    /// <summary>
    /// Plays a one-shot sound effect. Safe to call simultaneously with BGM.
    /// </summary>
    public void PlaySFX(AudioClip clip) {
        if (sfxSource != null && clip != null)
            sfxSource.PlayOneShot(clip);
    }

    /// <summary>
    /// Sets the background music volume (0 to 1).
    /// </summary>
    public void SetBGMVolume(float volume) {
        bgmVolume = Mathf.Clamp01(volume);
        if (bgmSource != null) bgmSource.volume = bgmVolume;
    }

    /// <summary>
    /// Sets the sound effects volume (0 to 1).
    /// </summary>
    public void SetSFXVolume(float volume) {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null) sfxSource.volume = sfxVolume;
    }
}
