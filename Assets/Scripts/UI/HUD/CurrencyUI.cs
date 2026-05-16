using TMPro;
using UnityEngine;

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

    #endregion

    #region UNITY

    private void Awake() {
        if (currencyText == null) {
            enabled = false;
        }
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
    /// Updates the currency display text with the new amount and triggers the scale pulse animation.
    /// </summary>
    /// <param name="newAmount">The new currency amount.</param>
    private void UpdateCurrencyDisplay(int newAmount) {
        if (currencyText == null) return;

        string formattedAmount = useThousandsSeparator
            ? newAmount.ToString("N0")
            : newAmount.ToString();

        currencyText.text = $"{prefix}{formattedAmount}";

        currencyText.GetComponent<TextScalePulse>()?.Pulse();
    }

    #endregion
}
