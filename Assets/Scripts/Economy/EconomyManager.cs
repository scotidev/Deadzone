using System;
using UnityEngine;

/// <summary>
/// Manages the player's currency system.
/// Tracks the player's money, handles transactions, and notifies listeners of changes.
/// </summary>
public class EconomyManager : MonoBehaviour {

    #region STATIC

    public static EconomyManager Instance { get; private set; }

    #endregion

    #region SERIALIZED FIELDS

    [Header("Starting Currency")]
    [SerializeField] private int startingCurrency = 30000;

    #endregion

    #region FIELDS

    private int currentCurrency;

    #endregion

    #region PROPERTIES

    public int GetCurrentCurrency() => currentCurrency;

    #endregion

    #region EVENTS

    public event Action<int> OnCurrencyChanged;
    public event Action<int, int> OnPurchaseFailed;

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

    private void InitializeCurrency() {
        currentCurrency = startingCurrency;

        OnCurrencyChanged?.Invoke(currentCurrency);
    }

    public void AddCurrency(int amount) {
        if (amount <= 0) {
            return;
        }

        currentCurrency += amount;

        OnCurrencyChanged?.Invoke(currentCurrency);
    }

    public bool TrySpendCurrency(int cost) {
        if (cost < 0) {
            return false;
        }

        if (cost == 0) {
            return true;
        }

        if (currentCurrency < cost) {
            OnPurchaseFailed?.Invoke(cost, currentCurrency);
            return false;
        }

        currentCurrency -= cost;

        OnCurrencyChanged?.Invoke(currentCurrency);

        return true;
    }

    public bool CanAfford(int cost) {
        return currentCurrency >= cost;
    }

    public void ResetCurrency() {
        currentCurrency = startingCurrency;
        OnCurrencyChanged?.Invoke(currentCurrency);
    }

    #endregion
}
