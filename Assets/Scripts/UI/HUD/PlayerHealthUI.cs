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

    // Private field to store the target fill amount for smooth lerping.
    // This allows the Update method to gradually move toward the target value.
    private float targetFillAmount;
    
    // Track if we've initialized the health bar yet.
    // This prevents re-initialization on every Start call.
    private bool hasInitialized;

    private void Awake() {
        // Validate that all required references are assigned in the Inspector.
        // If any reference is missing, we log an error and disable the script
        // to prevent null reference exceptions during gameplay.
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

        // Subscribe to the PlayerHealth events so this script gets notified
        // whenever health changes. The += operator adds this method as a listener.
        // When OnHealthChanged fires, OnHealthChanged(float) is called automatically.
        playerHealth.OnHealthChanged += OnHealthChanged;
        playerHealth.OnPlayerDied += OnPlayerDeath;
        
        // Initialize fill amounts to 1.0 (full health) as a safe default.
        // This ensures that even if PlayerHealth hasn't initialized yet,
        // the bar will show green instead of red.
        greenBar.fillAmount = 1f;
        redBar.fillAmount = 1f;
        targetFillAmount = 1f;
    }

    private void Start() {
        // Deferred initialization to ensure PlayerHealth has initialized first.
        // In Start(), all Awake() calls have completed, so PlayerHealth.currentHealth
        // is guaranteed to be set to maxHealth.
        if (!hasInitialized) {
            float initialHealth = playerHealth.GetHealthFraction();
            SetHealthInstant(initialHealth);
            hasInitialized = true;
            
            // DEBUG: Log the initial health value to verify it's correct
            Debug.Log($"[PlayerHealthUI] Initialized in Start() with health fraction: {initialHealth}, greenBar.fillAmount: {greenBar.fillAmount}");
        }
    }

    private void Update() {
        // Only perform smooth lerping if the useSmoothTransition option is enabled.
        // This Update method is only called when smooth transitions are active.
        if (!useSmoothTransition) return;

        UpdateHealthFill();
    }

    private void OnDestroy() {
        // Unsubscribe from events to prevent memory leaks.
        // This is important — if we don't unsubscribe, PlayerHealth will keep
        // a reference to this script even after it's destroyed, causing issues.
        // The -= operator removes this method from the event's listener list.
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
            // Set the target fill amount. The Update method will gradually move
            // greenBar.fillAmount toward this value using Mathf.Lerp.
            // This creates a smooth animation effect.
            targetFillAmount = healthFraction;
            
            // BUG FIX: Also update the current fill amount if it hasn't been initialized yet.
            // This prevents the bar from showing as red (fillAmount = 0) when transitioning
            // from initialization to the first damage. If the current fill is significantly
            // different from the target, we snap it close to prevent visual jumps.
            if (Mathf.Abs(greenBar.fillAmount - targetFillAmount) > 0.1f) {
                greenBar.fillAmount = healthFraction;
            }
        } else {
            // Bypass lerping — update the bar instantly.
            // This is useful for gameplay where immediate feedback is preferred.
            greenBar.fillAmount = healthFraction;
        }
    }

    /// <summary>
    /// Smoothly updates the green bar fill amount toward the target value.
    /// This method is called every frame only when useSmoothTransition is enabled.
    /// It uses Mathf.Lerp to create a smooth animation between the current and target values.
    /// </summary>
    private void UpdateHealthFill() {
        // Mathf.Lerp(a, b, t) linearly interpolates between a and b.
        // When t = 0, result = a. When t = 1, result = b.
        // When t = 0.5, result is halfway between a and b.
        // 
        // Time.deltaTime is the time in seconds since the last frame.
        // Multiplying lerpSpeed * Time.deltaTime ensures the animation
        // runs at a consistent speed regardless of frame rate.
        // If lerpSpeed = 5, and deltaTime = 0.016 (60 FPS), then t = 0.08 per frame.
        float currentFill = greenBar.fillAmount;
        float newFill = Mathf.Lerp(currentFill, targetFillAmount, Time.deltaTime * lerpSpeed);
        greenBar.fillAmount = newFill;
    }

    /// <summary>
    /// Sets the health bar to a specific fill amount instantly, bypassing any lerping.
    /// Useful for initialization at startup or when you need immediate visual feedback.
    /// </summary>
    public void SetHealthInstant(float healthFraction) {
        // Clamp the value between 0 and 1 to ensure it's a valid fill amount.
        // Mathf.Clamp01 is a convenience function for clamping between 0 and 1.
        float clampedFraction = Mathf.Clamp01(healthFraction);
        
        // Update both the current fill and the target fill to the same value.
        // This ensures that if smooth transitions are enabled, the bar doesn't
        // animate from the old value to the new value on the next frame.
        greenBar.fillAmount = clampedFraction;
        targetFillAmount = clampedFraction;
        
        // The red bar always stays at 1.0 (100% width) as a background.
        // This ensures the red bar is always visible showing the missing health.
        redBar.fillAmount = 1f;
    }

    /// <summary>
    /// Called when the player dies. Can be extended to add visual effects
    /// like fading, color changes, or disabling the health bar.
    /// </summary>
    private void OnPlayerDeath() {
        // Optional: Add death visual effects here in the future.
        // For example: Fade out the health bar, change color, play animation, etc.
        Debug.Log("[PlayerHealthUI] Player died. Health bar is now at 0.");
        
        // FEATURE: Turn the plus icon red when the player dies.
        // This provides visual feedback that the player is dead.
        // We check if plusIcon was assigned before trying to change its color
        // to avoid null reference errors.
        if (plusIcon != null) {
            // Color.red is a built-in Unity color representing pure red (1, 0, 0, 1).
            // This changes the plus_img's tint to red, indicating death state.
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
