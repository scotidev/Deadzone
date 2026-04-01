using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the player health bar UI. Subscribes to PlayerHealth events and updates
/// the green bar fill amount in real-time when the player takes damage or heals.
/// The red bar acts as a background showing the lost health.
/// </summary>
public class PlayerHealthUI : MonoBehaviour {

    [Header("Health References")]
    [Tooltip("Reference to the PlayerHealth script on the player GameObject.")]
    [SerializeField] private PlayerHealth playerHealth;

    [Tooltip("The Image component that represents current health (green bar).")]
    [SerializeField] private Image greenBar;

    [Tooltip("The Image component that represents missing health (red background).")]
    [SerializeField] private Image redBar;

    [Tooltip("Optional decorative health icon image.")]
    [SerializeField] private Image plusIcon;

    [Header("Animation Settings")]
    [Tooltip("Speed of health bar fill animation when using smooth transitions.")]
    [SerializeField] private float lerpSpeed = 5f;

    [Tooltip("Enable smooth lerp transitions instead of instant updates.")]
    [SerializeField] private bool useSmoothTransition = true;

    private float targetFillAmount;

    private bool hasInitialized;

    private void Awake() {
        if (playerHealth == null) {
            Debug.LogError("[PlayerHealthUI] PlayerHealth reference is missing! Assign it in the Inspector.");
            enabled = false;
            return;
        }

        if (greenBar == null) {
            Debug.LogError("[PlayerHealthUI] GreenBar Image reference is missing! Assign it in the Inspector.");
            enabled = false;
            return;
        }

        if (redBar == null) {
            Debug.LogError("[PlayerHealthUI] RedBar Image reference is missing! Assign it in the Inspector.");
            enabled = false;
            return;
        }

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

    /// <summary>
    /// Called whenever PlayerHealth.OnHealthChanged is invoked.
    /// This method receives the health fraction (0.0 to 1.0) and updates the bar accordingly.
    /// If smooth transitions are enabled, it sets the target and lets Update lerp to it.
    /// If smooth transitions are disabled, it updates the bar instantly.
    /// </summary>
    private void OnHealthChanged(float healthFraction) {
        if (useSmoothTransition) {
            targetFillAmount = healthFraction;

            if (Mathf.Abs(greenBar.fillAmount - targetFillAmount) > 0.1f) {
                greenBar.fillAmount = healthFraction;
            }
        }
        else {
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
}
