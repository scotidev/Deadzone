using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Persistent HUD panel that displays wave information during gameplay.
/// </summary>
public class WaveUI : BaseUI {

    #region SERIALIZED FIELDS

    [Header("Wave Information")]
    [SerializeField] private TMP_Text waveNumberText;
    [SerializeField] private TMP_Text waveClearText;
    [SerializeField] private TMP_Text enemiesRemainingText;
    [SerializeField] private TMP_Text timerText;

    [Header("Wave Announcement Animation")]
    [SerializeField] private float announcementFadeInSeconds = 0.2f;
    [SerializeField] private float announcementVisibleSeconds = 1.5f;
    [SerializeField] private float announcementFadeOutSeconds = 0.25f;

    [Header("Penguin Wave")]
    [Tooltip("Texto TMPro para o anúncio 'PENGUIN WAVE'. Crie um TextMeshPro no HUD e arraste aqui.")]
    [SerializeField] private TMP_Text penguinWaveText;

    [Header("Timer Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = Color.red;
    [SerializeField] private float warningThreshold = 15f;

    #endregion

    #region FIELDS

    private Coroutine waveStartAnnouncementRoutine;
    private Coroutine waveClearAnnouncementRoutine;
    private Coroutine penguinAnnouncementRoutine;

    #endregion

    #region UNITY

    protected override void Start() {
        base.Start();
        Show();

        HideAnnouncementText(waveNumberText);
        HideAnnouncementText(waveClearText);
        HideAnnouncementText(penguinWaveText);
        UpdateEnemiesRemaining(0);
        UpdateTimerDisplay(0, false);
    }

    protected override void Update() {
        base.Update();
        HandleTimerUpdate();
    }

    /// <summary>
    /// Fetches the current timer state from WaveManager and updates the UI.
    /// </summary>
    private void HandleTimerUpdate() {
        if (WaveManager.Instance == null || timerText == null) return;

        float currentTime = WaveManager.Instance.WaveTimer;
        bool isWaveActive = WaveManager.Instance.IsWaveActive;
        bool isCountdown = WaveManager.Instance.IsCountdownActive;

        if (isWaveActive || isCountdown) {
            UpdateTimerDisplay(currentTime, isWaveActive);
        } else {
            timerText.text = "";
        }
    }

    /// <summary>
    /// Formats and displays the timer text based on the current game state.
    /// Includes color interpolation during countdown warning phase.
    /// </summary>
    private void UpdateTimerDisplay(float time, bool activeWave) {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";

        if (!activeWave && WaveManager.Instance.IsCountdownActive && time <= warningThreshold) {
            float t = 1f - (time / warningThreshold);
            timerText.color = Color.Lerp(normalColor, warningColor, t);
        } else {
            timerText.color = normalColor;
        }
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
    /// Shows the "PENGUIN WAVE" announcement using the dedicated penguinWaveText.
    /// Called by EasterEggTarget when the easter egg activates, and by WaveManager
    /// when a new wave starts while PenguinMode is active.
    /// </summary>
    public void ShowPenguinWaveAnnouncement() {
        if (penguinWaveText == null)
            return;

        StopPenguinWaveAnnouncement();

        penguinAnnouncementRoutine = StartCoroutine(
            PlayAnnouncementSequence(penguinWaveText, "PENGUIN WAVE", OnPenguinAnnouncementFinished));
    }

    /// <summary>
    /// Stops any running penguin announcement coroutine and hides the text.
    /// </summary>
    private void StopPenguinWaveAnnouncement() {
        if (penguinAnnouncementRoutine != null) {
            StopCoroutine(penguinAnnouncementRoutine);
            penguinAnnouncementRoutine = null;
        }

        HideAnnouncementText(penguinWaveText);
    }

    /// <summary>
    /// Marks the penguin announcement routine as finished.
    /// </summary>
    private void OnPenguinAnnouncementFinished() {
        penguinAnnouncementRoutine = null;
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
