using UnityEngine;

// ==============================================================
//  DESIGN PATTERN: FACADE
// ==============================================================
//  A Facade provides a simplified, stable interface to a subsystem
//  that may change internally over time.
//
//  Here, SlowMotionManager is a thin facade over GameManager:
//  any script that needs slow motion simply calls
//      SlowMotionManager.Instance?.TriggerSlowMotion(1.0f);
//  without knowing anything about coroutines, fixedDeltaTime, or
//  how time scale is actually managed.
//
//  BENEFIT: If the slow motion implementation inside GameManager
//  changes (e.g. we add a lerp transition), every caller remains
//  completely untouched — they still talk to SlowMotionManager.
//
//  RULE: This class must NEVER write to Time.timeScale directly.
//  All time manipulation must go through GameManager.
// ==============================================================

/// <summary>
/// Singleton facade for the slow motion system.
/// <para>
/// Exposes the <see cref="TriggerSlowMotion"/> entry point used by
/// gameplay scripts (e.g. <c>ExplosiveBarrelScript</c>), while
/// delegating all actual time scale manipulation to <see cref="GameManager"/>,
/// which is the single source of truth for <c>Time.timeScale</c>.
/// </para>
/// <para>Usage: <c>SlowMotionManager.Instance?.TriggerSlowMotion(1.0f);</c></para>
/// </summary>
public class SlowMotionManager : MonoBehaviour {

    // ==============================================================
    //  SINGLETON
    // ==============================================================
    //  "public static" → any script can read this.
    //  "private set"   → only this class can assign it.
    //  The null-conditional "?" on Instance lets callers skip the
    //  call safely if this manager is not present in the scene.
    // ==============================================================

    /// <summary>
    /// Global access point to the single <see cref="SlowMotionManager"/> instance.
    /// </summary>
    public static SlowMotionManager Instance { get; private set; }

    private void Awake() {
        // If another instance already exists, this one is a duplicate
        // and should be destroyed immediately.
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ==============================================================
    //  PUBLIC API  —  entry point for gameplay scripts
    // ==============================================================
    //  This is the ONLY method outside scripts should call.
    //  It intentionally contains zero time-scale logic — its sole
    //  job is to forward the request to GameManager and let it
    //  handle the coroutine, fixedDeltaTime, and restoration.
    // ==============================================================

    /// <summary>
    /// Triggers a slow motion effect lasting <paramref name="realDuration"/>
    /// seconds of real (wall-clock) time.
    /// <para>
    /// If slow motion is already active the timer resets, so chain
    /// explosions naturally extend the effect rather than overlapping it.
    /// </para>
    /// </summary>
    /// <param name="realDuration">
    /// Duration in real seconds. Not affected by <c>Time.timeScale</c>.
    /// </param>
    public void TriggerSlowMotion(float realDuration) {
        // Delegate entirely to GameManager — this class owns no logic.
        // GameManager is the single source of truth for Time.timeScale.
        GameManager.Instance?.TriggerSlowMotion(realDuration);
    }
}
