using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the player health bar UI. Subscribes to PlayerHealth events and updates
/// the green bar fill amount in real-time when the player takes damage or heals.
/// The red bar acts as a background showing the lost health.
/// </summary>
public class PlayerHealthUI : MonoBehaviour {

    #region SERIALIZED FIELDS

    [Header("Health References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image greenBar;
    [SerializeField] private Image redBar;
    [SerializeField] private Image plusIcon;

    [Header("Animation Settings")]
    [SerializeField] private float lerpSpeed = 5f;
    [SerializeField] private bool useSmoothTransition = true;

    #endregion

    #region FIELDS

    private float targetFillAmount;
    private bool hasInitialized;

    #endregion

    #region UNITY

    private void Awake() {
        playerHealth.OnHealthChanged += OnHealthChanged;
        playerHealth.OnPlayerDied += OnPlayerDeath;

        greenBar.fillAmount = 1f;
        redBar.fillAmount = 1f;
        targetFillAmount = 1f;
    }

    private void Start() {
        if (!hasInitialized) {
            float initialHealth = playerHealth.GetHealthFraction();
            SetHealthInstant(initialHealth);
            hasInitialized = true;
        }
    }

    private void Update() {
        if (!useSmoothTransition) return;

        UpdateHealthFill();
    }

    private void OnDestroy() {
        if (playerHealth != null) {
            playerHealth.OnHealthChanged -= OnHealthChanged;
            playerHealth.OnPlayerDied -= OnPlayerDeath;
        }
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Called whenever PlayerHealth.OnHealthChanged is invoked.
    /// This method receives the health fraction (0.0 to 1.0) and updates the bar accordingly.
    /// </summary>
    private void OnHealthChanged(float healthFraction) {
        if (useSmoothTransition) {
            targetFillAmount = healthFraction;

            if (Mathf.Abs(greenBar.fillAmount - targetFillAmount) > 0.1f) {
                greenBar.fillAmount = healthFraction;
            }
        } else {
            greenBar.fillAmount = healthFraction;
        }
    }

    /// <summary>
    /// Smoothly updates the green bar fill amount toward the target value.
    /// This method is called every frame only when useSmoothTransition is enabled.
    /// It uses Mathf.Lerp to create a smooth animation between the current and target values.
    /// </summary>
    private void UpdateHealthFill() {
        float currentFill = greenBar.fillAmount;
        float newFill = Mathf.Lerp(currentFill, targetFillAmount, Time.deltaTime * lerpSpeed);
        greenBar.fillAmount = newFill;
    }

    /// <summary>
    /// Sets the health bar to a specific fill amount instantly, bypassing any lerping.
    /// Useful for initialization at startup or when you need immediate visual feedback.
    /// </summary>
    public void SetHealthInstant(float healthFraction) {
        float clampedFraction = Mathf.Clamp01(healthFraction);

        greenBar.fillAmount = clampedFraction;
        targetFillAmount = clampedFraction;

        redBar.fillAmount = 1f;
    }

    /// <summary>
    /// Called when the player dies. Can be extended to add visual effects
    /// like fading, color changes, or disabling the health bar.
    /// </summary>
    private void OnPlayerDeath() {
        if (plusIcon != null) {
            plusIcon.color = Color.red;
        }
    }

    /// <summary>
    /// Public method to get the current health fill amount (0.0 to 1.0).
    /// Useful for other systems that need to know the health bar state.
    /// </summary>
    public float GetCurrentFillAmount() {
        return greenBar.fillAmount;
    }

    /// <summary>
    /// Public method to get the target fill amount (0.0 to 1.0).
    /// Useful for debugging or checking where the health bar is animating toward.
    /// </summary>
    public float GetTargetFillAmount() {
        return targetFillAmount;
    }

    #endregion
}
