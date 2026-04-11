using UnityEngine;
using InfimaGames.LowPolyShooterPack;

/// <summary>
/// Legacy compatibility wrapper that forwards audio calls to <see cref="IAudioManagerService"/>.
/// This keeps old scene references safe while the real implementation stays centralized in AudioManagerService.
/// </summary>
[System.Obsolete("Use IAudioManagerService via ServiceLocator.Current.Get<IAudioManagerService>() instead.")]
public class AudioManager : MonoBehaviour
{
    /// <summary>Global access point to the single <see cref="AudioManager"/> instance.</summary>
    public static AudioManager Instance { get; private set; }

    /// <summary>
    /// Cached reference to the unified audio service.
    /// </summary>
    private IAudioManagerService audioService;

    /// <summary>
    /// Exposes the current BGM volume from the unified audio service.
    /// </summary>
    public float BGMVolume => audioService?.GetBGMVolume() ?? 0f;

    /// <summary>
    /// Exposes the current SFX volume from the unified audio service.
    /// </summary>
    public float SFXVolume => audioService?.GetSFXVolume() ?? 0f;

    /// <summary>
    /// Initializes singleton compatibility and resolves the unified audio service.
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ResolveAudioService();
            return;
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// Resolves the centralized audio service through the Service Locator.
    /// </summary>
    private void ResolveAudioService()
    {
        // First principle: this wrapper should never own audio state, only delegate to the single source of truth.
        audioService ??= ServiceLocator.Current.Get<IAudioManagerService>();
    }

    /// <summary>
    /// Plays a BGM clip through the centralized audio service.
    /// </summary>
    /// <param name="clip">Music clip to play.</param>
    /// <param name="loop">True to loop playback.</param>
    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        ResolveAudioService();
        audioService?.PlayBGM(clip, loop);
    }

    /// <summary>
    /// Stops BGM playback through the centralized audio service.
    /// </summary>
    public void StopBGM()
    {
        ResolveAudioService();
        audioService?.StopBGM();
    }

    /// <summary>
    /// Plays a non-spatial one-shot SFX through the centralized audio service.
    /// </summary>
    /// <param name="clip">SFX clip to play.</param>
    /// <param name="volumeScale">Per-call volume multiplier.</param>
    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        ResolveAudioService();
        audioService?.PlaySFX2D(clip, volumeScale);
    }

    /// <summary>
    /// Sets BGM volume through the centralized audio service.
    /// </summary>
    /// <param name="volume">Volume value in the [0, 1] range.</param>
    public void SetBGMVolume(float volume)
    {
        ResolveAudioService();
        audioService?.SetBGMVolume(volume);
    }

    /// <summary>
    /// Sets SFX volume through the centralized audio service.
    /// </summary>
    /// <param name="volume">Volume value in the [0, 1] range.</param>
    public void SetSFXVolume(float volume)
    {
        ResolveAudioService();
        audioService?.SetSFXVolume(volume);
    }
}
