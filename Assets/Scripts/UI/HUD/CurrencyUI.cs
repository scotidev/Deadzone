using System.Collections;
using TMPro;
using UnityEngine;

// REFATORAÇÃO: esse script nao deveria herdar de ElementText? analise necessaria.

/// <summary>
/// UI component that displays the player's current currency in the HUD.
/// Subscribes to EconomyManager events to update in real-time.
/// </summary>
public class CurrencyUI : MonoBehaviour {

    #region SERIALIZED FIELDS

    [Header("UI References")]

    [SerializeField] private TextMeshProUGUI currencyText;

    [Header("Display Format")]

    [SerializeField] private string prefix = "$";
    [SerializeField] private bool useThousandsSeparator = true;

    [Header("Animation (Optional)")]

    [Tooltip("If true, currency text will briefly scale up when it changes.")]
    [SerializeField] private bool animateOnChange = true;
    [SerializeField] private float animationScale = 1.2f;
    [SerializeField] private float animationDuration = 0.2f;

    #endregion

    #region FIELDS

    private Vector3 originalScale;
    private Coroutine animationCoroutine;

    #endregion

    #region UNITY

    private void Awake() {
        if (currencyText == null) {
            enabled = false;
            return;
        }

        originalScale = currencyText.transform.localScale;
    }

    private void Start() {
        if (EconomyManager.Instance != null) {
            EconomyManager.Instance.OnCurrencyChanged += UpdateCurrencyDisplay;

            UpdateCurrencyDisplay(EconomyManager.Instance.GetCurrentCurrency());
        }
    }

    private void OnDestroy() {
        if (EconomyManager.Instance != null) {
            EconomyManager.Instance.OnCurrencyChanged -= UpdateCurrencyDisplay;
        }
    }

    private void OnValidate() {
        if (currencyText == null) {
            currencyText = GetComponent<TextMeshProUGUI>();
        }
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Updates the currency display text with the new amount, applying formatting and triggering animation if enabled.
    /// </summary>
    /// <param name="newAmount">The new currency amount.</param>
    private void UpdateCurrencyDisplay(int newAmount) {
        if (currencyText == null) return;

        string formattedAmount = useThousandsSeparator
            ? newAmount.ToString("N0")
            : newAmount.ToString();

        currencyText.text = $"{prefix}{formattedAmount}";

        if (animateOnChange && gameObject.activeInHierarchy) {
            TriggerAnimation();
        }
    }

    /// <summary>
    /// Starts the text animation by stopping any currently running animation and initiating a new animation sequence.
    /// </summary>
    private void TriggerAnimation() {
        if (animationCoroutine != null) {
            StopCoroutine(animationCoroutine);
        }
        animationCoroutine = StartCoroutine(AnimateTextRoutine());
    }

    /// <summary>
    /// Coroutine that smoothly scales the currency text up to the specified animation scale and then back down to its original scale over the defined duration.
    /// </summary>
    private IEnumerator AnimateTextRoutine() {
        float timer = 0f;
        while (timer < animationDuration) {
            timer += Time.deltaTime;
            float progress = timer / animationDuration;
            float currentScale = Mathf.Lerp(animationScale, 1f, progress);
            currencyText.transform.localScale = originalScale * currentScale;
            yield return null;
        }
        currencyText.transform.localScale = originalScale;
        animationCoroutine = null;
    }

    #endregion
}
