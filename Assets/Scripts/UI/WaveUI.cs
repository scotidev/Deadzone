using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Persistent HUD panel that displays wave information during gameplay.
/// </summary>
public class WaveUI : BaseUI {

    [Header("Wave Information Texts")]
    [Tooltip("Temporary label that appears when a new wave starts. Example: 'Wave 3'.")]
    [SerializeField] private TMP_Text waveNumberText;

    [Tooltip("Temporary label that appears when a wave is completed.")]
    [SerializeField] private TMP_Text waveClearText;

    [Tooltip("Displays how many enemies are still alive in this wave.")]
    [SerializeField] private TMP_Text enemiesRemainingText;

    [Header("Wave Announcement Animation")]
    [Tooltip("Duration (in seconds) for fade in.")]
    [Min(0f)]
    [SerializeField] private float announcementFadeInSeconds = 0.2f;

    [Tooltip("Duration (in seconds) to keep the label fully visible.")]
    [Min(0f)]
    [SerializeField] private float announcementVisibleSeconds = 1.5f;

    [Tooltip("Duration (in seconds) for fade out.")]
    [Min(0f)]
    [SerializeField] private float announcementFadeOutSeconds = 0.25f;

    private Coroutine waveStartAnnouncementRoutine;
    private Coroutine waveClearAnnouncementRoutine;

    /// <summary>
    /// Initializes the HUD and prepares temporary announcement labels hidden by default.
    /// </summary>
    protected override void Start() {
        base.Start();
        Show();

        if (waveNumberText == null)
            Debug.LogError("[WaveUI] 'Wave Number Text' not assigned in the Inspector!");

        if (waveClearText == null)
            Debug.LogError("[WaveUI] 'Wave Clear Text' not assigned in the Inspector!");

        if (enemiesRemainingText == null)
            Debug.LogError("[WaveUI] 'Enemies Remaining Text' not assigned in the Inspector!");

        HideAnnouncementText(waveNumberText);
        HideAnnouncementText(waveClearText);
        UpdateEnemiesRemaining(0);
    }

    /// <summary>
    /// Legacy entry point kept for existing callers.
    /// Triggers the temporary "Wave X" announcement animation.
    /// </summary>
    public void UpdateWaveNumber(int wave) {
        ShowWaveStartAnnouncement(wave);
    }

    /// <summary>
    /// Shows the "Wave X" announcement with fade-in and fade-out animation.
    /// </summary>
    public void ShowWaveStartAnnouncement(int wave) {
        if (wave <= 0 || waveNumberText == null)
            return;

        StopWaveClearAnnouncement();
        StopWaveStartAnnouncement();

        // The coroutine runs over multiple frames so alpha transitions happen smoothly.
        waveStartAnnouncementRoutine = StartCoroutine(
            PlayAnnouncementSequence(waveNumberText, $"Wave {wave}", OnWaveStartAnnouncementFinished));
    }

    /// <summary>
    /// Shows the "Wave Clear" announcement with the same animation profile.
    /// </summary>
    public void ShowWaveClearAnnouncement() {
        if (waveClearText == null)
            return;

        StopWaveStartAnnouncement();
        StopWaveClearAnnouncement();

        // Reusing the same sequence keeps both announcements consistent and maintainable.
        waveClearAnnouncementRoutine = StartCoroutine(
            PlayAnnouncementSequence(waveClearText, "Wave Clear", OnWaveClearAnnouncementFinished));
    }

    /// <summary>
    /// Updates the enemies remaining label.
    /// Called by the WaveManager each time an enemy dies.
    /// </summary>
    public void UpdateEnemiesRemaining(int count) {
        if (enemiesRemainingText != null)
            enemiesRemainingText.text = $"Enemies {count}";
    }

    /// <summary>
    /// Executes the full announcement lifecycle: show, fade in, hold, fade out, and hide.
    /// </summary>
    private IEnumerator PlayAnnouncementSequence(TMP_Text targetText, string message, System.Action onFinished) {
        targetText.text = message;
        targetText.gameObject.SetActive(true);
        SetTextAlpha(targetText, 0f);

        // Fade-in uses interpolation, which converts elapsed time into smooth alpha progression.
        yield return FadeTextAlpha(targetText, 0f, 1f, announcementFadeInSeconds);

        if (announcementVisibleSeconds > 0f)
            yield return new WaitForSecondsRealtime(announcementVisibleSeconds);

        // Fade-out mirrors fade-in so the element exits without abrupt visual jumps.
        yield return FadeTextAlpha(targetText, 1f, 0f, announcementFadeOutSeconds);

        targetText.gameObject.SetActive(false);
        onFinished?.Invoke();
    }

    /// <summary>
    /// Interpolates text alpha over time using unscaled delta time for HUD consistency.
    /// </summary>
    private IEnumerator FadeTextAlpha(TMP_Text targetText, float from, float to, float duration) {
        if (duration <= 0f) {
            SetTextAlpha(targetText, to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration) {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            SetTextAlpha(targetText, Mathf.Lerp(from, to, progress));
            yield return null;
        }

        SetTextAlpha(targetText, to);
    }

    /// <summary>
    /// Applies a new alpha value preserving the original RGB text color.
    /// </summary>
    private static void SetTextAlpha(TMP_Text targetText, float alpha) {
        Color color = targetText.color;
        color.a = alpha;
        targetText.color = color;
    }

    /// <summary>
    /// Hides an announcement text immediately and resets its alpha.
    /// </summary>
    private void HideAnnouncementText(TMP_Text targetText) {
        if (targetText == null)
            return;

        SetTextAlpha(targetText, 0f);
        targetText.gameObject.SetActive(false);
    }

    /// <summary>
    /// Stops any running start-wave announcement coroutine and hides its text.
    /// </summary>
    private void StopWaveStartAnnouncement() {
        if (waveStartAnnouncementRoutine != null) {
            StopCoroutine(waveStartAnnouncementRoutine);
            waveStartAnnouncementRoutine = null;
        }

        HideAnnouncementText(waveNumberText);
    }

    /// <summary>
    /// Stops any running clear-wave announcement coroutine and hides its text.
    /// </summary>
    private void StopWaveClearAnnouncement() {
        if (waveClearAnnouncementRoutine != null) {
            StopCoroutine(waveClearAnnouncementRoutine);
            waveClearAnnouncementRoutine = null;
        }

        HideAnnouncementText(waveClearText);
    }

    /// <summary>
    /// Marks the start-wave routine as finished so a new one can be started cleanly.
    /// </summary>
    private void OnWaveStartAnnouncementFinished() {
        waveStartAnnouncementRoutine = null;
    }

    /// <summary>
    /// Marks the clear-wave routine as finished so a new one can be started cleanly.
    /// </summary>
    private void OnWaveClearAnnouncementFinished() {
        waveClearAnnouncementRoutine = null;
    }
}
