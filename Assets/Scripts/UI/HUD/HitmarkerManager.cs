using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the hitmarker system for the game. Displays visual feedback and plays audio
/// when the player successfully hits an enemy.
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

    private WaitForSeconds hitmarkerDelay;

    private void Awake() {
        hitmarkerDelay = new WaitForSeconds(displayDuration);
    }

    private void OnEnable() {
        OnEnemyHit += ShowHitmarker;
    }

    private void OnDisable() {
        OnEnemyHit -= ShowHitmarker;
    }

    /// <summary>
    /// Static method that allows external scripts (like Projectile) to trigger the hitmarker.
    /// This is called from Projectile.cs when an enemy is successfully hit.
    /// </summary>
    public static void TriggerHitmarker() {
        OnEnemyHit?.Invoke();
    }

    /// <summary>
    /// Shows the hitmarker by enabling its UI element and playing the sound effect.
    /// Called by the OnEnemyHit event when a projectile hits an enemy.
    /// </summary>
    private void ShowHitmarker() {
        if (hitmarkerImage == null) {
            return;
        }

        hitmarkerImage.enabled = true;

        if (AudioManager.Instance != null && hitmarkerSound != null) {
            AudioManager.Instance.PlaySFX(hitmarkerSound, 1f);
        }

        StopAllCoroutines();

        StartCoroutine(HideHitmarkerAfterDelay());
    }

    /// <summary>
    /// Coroutine that waits for the display duration, then hides the hitmarker.
    /// </summary>
    private IEnumerator HideHitmarkerAfterDelay() {
        yield return hitmarkerDelay;

        if (hitmarkerImage != null) {
            hitmarkerImage.enabled = false;
        }
    }
}
