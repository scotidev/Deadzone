using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the hitmarker system for the game. Displays visual feedback and plays audio
/// when the player successfully hits an enemy. This creates satisfying "hit confirmation"
/// feedback that improves combat feel and responsiveness.
/// </summary>
public class HitmarkerManager : MonoBehaviour {

    /// <summary>
    /// Static event that is invoked when an enemy is hit by a projectile.
    /// The Projectile script invokes this event through the TriggerHitmarker() method,
    /// and HitmarkerManager subscribes to it in OnEnable().
    /// </summary>
    private static event Action OnEnemyHit;

    [Header("UI References")]
    [Tooltip("Image component that displays the hitmarker visual.")]
    [SerializeField] private Image hitmarkerImage;

    [Header("Audio References")]
    [Tooltip("Audio clip to play when an enemy is hit.")]
    [SerializeField] private AudioClip hitmarkerSound;

    [Header("Settings")]
    [Tooltip("How long the hitmarker remains visible on screen (in seconds).")]
    [Range(0.05f, 0.5f)]
    [SerializeField] private float displayDuration = 0.1f;

    // Cache for WaitForSeconds to avoid GC allocation on each coroutine start
    // This is reused across multiple hitmarker displays for efficiency
    private WaitForSeconds hitmarkerDelay;

    private void Awake() {
        // Pre-calculate and cache the WaitForSeconds to avoid garbage allocation
        // in coroutines. This is a small optimization that prevents creating new
        // WaitForSeconds objects repeatedly during rapid-fire combat.
        hitmarkerDelay = new WaitForSeconds(displayDuration);
    }

    private void OnEnable() {
        // Subscribe to the hitmarker event when this script is enabled.
        // This follows the proper event lifecycle pattern: subscribe in OnEnable,
        // unsubscribe in OnDisable. This prevents memory leaks from orphaned events.
        OnEnemyHit += ShowHitmarker;
    }

    private void OnDisable() {
        // Unsubscribe from the hitmarker event when this script is disabled.
        // This ensures the event doesn't try to call a method on a disabled component.
        OnEnemyHit -= ShowHitmarker;
        Debug.Log("HitmarkerManager subscribed to OnEnemyHit event");
    }

    /// <summary>
    /// Static method that allows external scripts (like Projectile) to trigger the hitmarker.
    /// This is called from Projectile.cs when an enemy is successfully hit.
    /// </summary>
    public static void TriggerHitmarker() {
        // Invoke the OnEnemyHit event to notify all subscribers (ShowHitmarker method).
        // The null-conditional operator ?. ensures no error if no one is subscribed.
        OnEnemyHit?.Invoke();
    }

    /// <summary>
    /// Shows the hitmarker by enabling its UI element and playing the sound effect.
    /// Called by the OnEnemyHit event when a projectile hits an enemy.
    /// </summary>
    private void ShowHitmarker() {
        Debug.Log("HITMARKER TRIGGERED!");
        // Null checks ensure the system doesn't break if references are missing
        if (hitmarkerImage == null) {
            Debug.LogWarning("[HitmarkerManager] Hitmarker Image reference is missing!");
            return;
        }

        // Enable the Image component to make the hitmarker visible on screen
        // This works whether the component is disabled or the GameObject is inactive
        hitmarkerImage.enabled = true;

        // Play the hitmarker sound through the audio manager
        // AudioManager.Instance handles one-shot sounds across the SFX channel
        // volumeScale of 1f ensures consistent volume on every hit
        if (AudioManager.Instance != null && hitmarkerSound != null) {
            AudioManager.Instance.PlaySFX(hitmarkerSound, 1f);
        }

        // Stop any existing hide coroutine to prevent conflicts when hitting rapidly
        // This ensures each hit gets the full display duration
        StopAllCoroutines();
        
        // Start a coroutine to hide the hitmarker after the configured display duration
        // This prevents the hitmarker from staying on screen permanently
        StartCoroutine(HideHitmarkerAfterDelay());
    }

    /// <summary>
    /// Coroutine that waits for the display duration, then hides the hitmarker.
    /// </summary>
    private System.Collections.IEnumerator HideHitmarkerAfterDelay() {
        // Wait for the specified display duration using the cached WaitForSeconds.
        // This prevents garbage allocation that would happen with "new WaitForSeconds(displayDuration)"
        yield return hitmarkerDelay;

        // Hide the hitmarker by disabling the Image component
        // This keeps the GameObject active so the script continues running
        if (hitmarkerImage != null) {
            hitmarkerImage.enabled = false;
        }
    }
}
