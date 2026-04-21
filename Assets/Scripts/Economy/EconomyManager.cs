using System;
using UnityEngine;

// REFATORAÇÃO: Esse script deveria ser um Service do Service Locator? Analise mais profunda necessaria.
// REFATORAÇÃO: Adicionar feedbacks visuais/auditivos para o jogador quando uma compra falha (ex: som de erro, shake na UI, etc).

/// <summary>
/// Manages the player's currency system.
/// Tracks the player's money, handles transactions, and notifies listeners of changes.
/// </summary>
public class EconomyManager : MonoBehaviour {

    #region STATIC

    /// <summary>Global access point to the single <see cref="EconomyManager"/> instance.</summary>
    public static EconomyManager Instance { get; private set; }

    #endregion

    #region SERIALIZED FIELDS

    [Header("Starting Currency")]
    [SerializeField] private int startingCurrency = 30000;

    #endregion

    #region FIELDS

    private int currentCurrency;
    public event Action<int> OnCurrencyChanged;
    public event Action<int> OnPurchaseSuccess;
    public event Action<int, int> OnPurchaseFailed;

    #endregion

    #region PROPERTIES

    public int GetCurrentCurrency() => currentCurrency;

    #endregion

    #region UNITY

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(gameObject);
            return;
        }

        InitializeCurrency();
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Initializes the player's currency to the starting amount and notifies listeners.
    /// </summary>
    private void InitializeCurrency() {
        currentCurrency = startingCurrency;

        OnCurrencyChanged?.Invoke(currentCurrency);
    }

    /// <summary>
    /// Adds currency to the player's total.
    /// </summary>
    /// <param name="amount">The amount of currency to add. Must be positive.</param>
    public void AddCurrency(int amount) {
        if (amount <= 0) {
            return;
        }

        currentCurrency += amount;

        OnCurrencyChanged?.Invoke(currentCurrency);
    }

    /// <summary>
    /// Attempts to spend currency for a purchase.
    /// Returns true if the purchase succeeded, false if insufficient funds.
    /// </summary>
    /// <param name="cost">The cost of the item/upgrade.</param>
    /// <returns>True if purchase succeeded, false if insufficient funds.</returns>
    public bool TrySpendCurrency(int cost) {
        if (cost < 0) {
            return false;
        }

        if (cost == 0) {
            OnPurchaseSuccess?.Invoke(0);
            return true;
        }

        if (currentCurrency < cost) {
            // Adicionar feedback visual/auditivo para o jogador aqui, como um som de erro ou uma animação de shake na UI
            OnPurchaseFailed?.Invoke(cost, currentCurrency);
            return false;
        }

        currentCurrency -= cost;

        OnCurrencyChanged?.Invoke(currentCurrency);
        OnPurchaseSuccess?.Invoke(cost);

        return true;
    }

    /// <summary>
    /// Checks if the player can afford a specific cost without making the purchase.
    /// Useful for UI to show if a button should be enabled/disabled.
    /// </summary>
    /// <param name="cost">The cost to check.</param>
    /// <returns>True if player has enough currency.</returns>
    public bool CanAfford(int cost) {
        return currentCurrency >= cost;
    }

    /// <summary>
    /// Resets the player's currency to the starting amount.
    /// Useful for restarting the game or testing.
    /// </summary>
    public void ResetCurrency() {
        currentCurrency = startingCurrency;
        OnCurrencyChanged?.Invoke(currentCurrency);
    }

    #endregion
}
