using System.Collections;
using TMPro;
using UnityEngine;

// REFATORAÇÃO: esse script deveria implementar Element.cs? analise necessaria
// REFATORAÇÃO: esse script precisa ser um serviço do ServiceLocator? analise necessaria

/// <summary>
/// Displays temporary subtitle text for merchant dialogue lines.
/// </summary>
public class MerchantSubtitleUI : MonoBehaviour {

    #region STATIC

    /// <summary>Global access point to the single <see cref="MerchantSubtitleUI"/> instance.</summary>
    public static MerchantSubtitleUI Instance { get; private set; }

    #endregion

    #region SERIALIZED FIELDS

    [Header("References")]
    [SerializeField] private GameObject subtitleRoot;
    [SerializeField] private TextMeshProUGUI subtitleText;

    #endregion

    #region FIELDS

    private Coroutine hideCoroutine;

    #endregion

    #region UNITY

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else if (Instance != this) {
            Destroy(gameObject);
            return;
        }

        HideImmediate();
    }

    private void OnDestroy() {
        if (Instance == this)
            Instance = null;
    }

    #endregion

    #region METHODS


    /// <summary>
    /// Shows a subtitle for a fixed duration.
    /// </summary>
    /// <param name="subtitle">Subtitle text to display.</param>
    /// <param name="duration">Display duration in seconds.</param>
    public void ShowSubtitle(string subtitle, float duration) {
        if (subtitleRoot == null || subtitleText == null)
            return;

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
    public void HideImmediate() {
        if (hideCoroutine != null) {
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
    private IEnumerator HideAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);
        HideImmediate();
    }

    #endregion
}
