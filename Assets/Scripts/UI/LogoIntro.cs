using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using InfimaGames.LowPolyShooterPack;

/// <summary>
/// Class responsible for the logo intro scene. It waits for a specified duration
/// before transitioning to the main menu. It also allows skipping the intro
/// with any key press or mouse click if enabled.
/// </summary>
public class LogoIntro : MonoBehaviour {

    #region SERIALIZED FIELDS

    [Header("Intro Settings")]

    [SerializeField] private float duration = 8f;
    [SerializeField] private bool allowSkip = true;

    [Header("Audio Config")]
    [SerializeField] private InfimaGames.LowPolyShooterPack.AudioSettings audioSettings = new InfimaGames.LowPolyShooterPack.AudioSettings(1.0f, 0.0f, true);

    [Header("ScotiDev Logo")]
    [SerializeField] private AudioClip audioLogoScotiDev;
    [SerializeField] private float delayScoti = 0.5f;

    [Header("Lary Logo")]
    [SerializeField] private AudioClip audioLogoLary;
    [SerializeField] private float delayLary = 1.5f;

    #endregion

    #region FIELDS

    private bool skipped = false;
    private VideoPlayer videoPlayer;
    private IAudioManagerService audioManagerService;

    #endregion

    #region UNITY

    private void Awake() {
        videoPlayer = GetComponentInChildren<VideoPlayer>(); 
    }

    private void Start() {
        audioManagerService = ServiceLocator.Current.Get<IAudioManagerService>();
        GameManager.Instance?.SetState(GameState.Intro);

        if (videoPlayer != null) {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = System.IO.Path.Combine(Application.streamingAssetsPath, "Intro.mp4");
            videoPlayer.Play();
        }

        StartCoroutine(WaitAndLoad());
        StartCoroutine(PlayAudioSequence());
    }

    private void Update() {
        if (allowSkip && !skipped && Input.anyKeyDown)
            SkipIntro();
    }

    #endregion

    #region METHODS

    private IEnumerator PlayAudioSequence() {
        if (videoPlayer != null) {
            while (!videoPlayer.isPrepared) yield return null;
        }

        yield return new WaitForSeconds(delayScoti);
        PlayGlobalSound(audioLogoScotiDev);

        yield return new WaitForSeconds(delayLary - delayScoti);
        PlayGlobalSound(audioLogoLary);
    }

    private void PlayGlobalSound(AudioClip clip) {
        if (clip == null || skipped || audioManagerService == null) return;

        audioManagerService.PlayOneShot(clip, audioSettings);
    }

    private IEnumerator WaitAndLoad() {

        if (videoPlayer != null) {
            while (!videoPlayer.isPrepared) yield return null;
        }

        yield return new WaitForSeconds(duration);

        if (!skipped) GoToMenu();
    }

    private void SkipIntro() {

        if (skipped) return;
        skipped = true;
        
        StopAllCoroutines();
        GoToMenu();
    }

    private void GoToMenu() {
        SceneLoader.Instance?.LoadScene("Menu");
    }

    #endregion
}
