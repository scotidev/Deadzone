using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Displays temporary subtitle text for merchant dialogue lines.
/// </summary>
public class MerchantSubtitleUI : MonoBehaviour
{
    /// <summary>
    /// Global access to the subtitle presenter.
    /// </summary>
    public static MerchantSubtitleUI Instance { get; private set; }

    [Header("References")]
    [Tooltip("Root object that contains the subtitle visuals (panel/background/text).")]
    [SerializeField] private GameObject subtitleRoot;

    [Tooltip("Text field used to display subtitle content.")]
    [SerializeField] private TextMeshProUGUI subtitleText;

    /// <summary>
    /// Active coroutine handling timed hide behavior.
    /// </summary>
    private Coroutine hideCoroutine;

    /// <summary>
    /// Initializes singleton and starts hidden.
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        HideImmediate();
    }

    /// <summary>
    /// Clears singleton when this instance is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Shows a subtitle for a fixed duration.
    /// </summary>
    /// <param name="subtitle">Subtitle text to display.</param>
    /// <param name="duration">Display duration in seconds.</param>
    public void ShowSubtitle(string subtitle, float duration)
    {
        if (subtitleRoot == null || subtitleText == null)
            return;

        // First principle: always reset pending hide timers so each new subtitle owns the full visibility window.
        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        subtitleText.text = subtitle;
        subtitleRoot.SetActive(true);

        float safeDuration = Mathf.Max(0.1f, duration);
        hideCoroutine = StartCoroutine(HideAfterDelay(safeDuration));
    }

    /// <summary>
    /// Hides subtitle visuals immediately.
    /// </summary>
    public void HideImmediate()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        if (subtitleRoot != null)
            subtitleRoot.SetActive(false);

        if (subtitleText != null)
            subtitleText.text = string.Empty;
    }

    /// <summary>
    /// Hides subtitle visuals after a delay.
    /// </summary>
    /// <param name="delay">Delay in seconds.</param>
    /// <returns>Coroutine enumerator.</returns>
    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideImmediate();
    }
}
