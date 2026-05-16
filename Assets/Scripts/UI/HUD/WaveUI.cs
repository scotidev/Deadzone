using System.Collections;
using TMPro;
using UnityEngine;

// Legacy entry point kept for existing callers.
// O QUE A LIINHA  quer dizer? esse método está obsoleto? analise necessaria

/// <summary>
/// Persistent HUD panel that displays wave information during gameplay.
/// </summary>
public class WaveUI : BaseUI {

    #region SERIALIZED FIELDS

    [Header("Wave Information")]
    [SerializeField] private TMP_Text waveNumberText;
    [SerializeField] private TMP_Text waveClearText;
    [SerializeField] private TMP_Text enemiesRemainingText;

    [Header("Wave Announcement Animation")]
    [SerializeField] private float announcementFadeInSeconds = 0.2f;
    [SerializeField] private float announcementVisibleSeconds = 1.5f;
    [SerializeField] private float announcementFadeOutSeconds = 0.25f;

    #endregion

    #region FIELDS

    private Coroutine waveStartAnnouncementRoutine;
    private Coroutine waveClearAnnouncementRoutine;

    #endregion

    #region UNITY

    protected override void Start() {
        base.Start();
        Show();

        HideAnnouncementText(waveNumberText);
        HideAnnouncementText(waveClearText);
        UpdateEnemiesRemaining(0);
    }

    #endregion

    #region METHODS

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

        waveClearAnnouncementRoutine = StartCoroutine(
            PlayAnnouncementSequence(waveClearText, "Wave Clear", OnWaveClearAnnouncementFinished));
    }

    /// <summary>
    /// Updates the enemies remaining label.
    /// Called by the WaveManager each time an enemy dies.
    /// </summary>
    public void UpdateEnemiesRemaining(int count) {
        if (enemiesRemainingText != null)
            enemiesRemainingText.text = $"{count} ENEMIES";
    }

    /// <summary>
    /// Executes the full announcement lifecycle: show, fade in, hold, fade out, and hide.
    /// </summary>
    private IEnumerator PlayAnnouncementSequence(TMP_Text targetText, string message, System.Action onFinished) {
        targetText.text = message;
        targetText.gameObject.SetActive(true);
        SetTextAlpha(targetText, 0f);

        yield return FadeTextAlpha(targetText, 0f, 1f, announcementFadeInSeconds);

        if (announcementVisibleSeconds > 0f)
            yield return new WaitForSecondsRealtime(announcementVisibleSeconds);

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

    #endregion
}
