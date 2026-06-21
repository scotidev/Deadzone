using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using InfimaGames.LowPolyShooterPack;

/// <summary>
/// Manages the hitmarker system for the game. Displays visual feedback and plays audio
/// when the player successfully hits an enemy.
/// </summary>
public class HitmarkerManager : MonoBehaviour {

    #region STATIC

    /// <summary>
    /// Static event that is invoked when an enemy is hit by a projectile.
    /// The Projectile script invokes this event through the TriggerHitmarker() method,
    /// and HitmarkerManager subscribes to it in OnEnable().
    /// </summary>
    private static event Action OnEnemyHit;

    #endregion

    #region SERIALIZED FIELDS

    [Header("UI References")]
    [SerializeField] private Image hitmarkerImage;

    [Header("Audio References")]
    [SerializeField] private AudioClip hitmarkerSound;

    [Header("Settings")]
    [Range(0.05f, 0.5f)]
    [SerializeField] private float displayDuration = 0.1f;

    #endregion

    #region FIELDS

    private IAudioManagerService audioService;
    private WaitForSeconds hitmarkerDelay;

    #endregion

    #region UNITY 

    private void Awake() {
        hitmarkerDelay = new WaitForSeconds(displayDuration);
        audioService = ServiceLocator.Current.Get<IAudioManagerService>();
    }

    private void OnEnable() {
        OnEnemyHit += ShowHitmarker;
    }

    private void OnDisable() {
        OnEnemyHit -= ShowHitmarker;
    }

    #endregion

    #region METHODS

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

        if (audioService != null && hitmarkerSound != null) {
            audioService.PlaySFX2D(hitmarkerSound, 1f);
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

    #endregion
}
