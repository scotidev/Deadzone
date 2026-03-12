using UnityEngine;

/// <summary>
/// Singleton facade for the slow motion system.
/// <para>
/// Exposes the <see cref="TriggerSlowMotion"/> entry point used by
/// gameplay scripts, while delegating all actual time scale manipulation to <see cref="GameManager"/>.
/// </para>
/// <para>Usage: <c>SlowMotionManager.Instance?.TriggerSlowMotion(1.0f);</c></para>
/// </summary>
public class SlowMotionManager : MonoBehaviour {

    /// <summary>
    /// Global access point to the single <see cref="SlowMotionManager"/> instance.
    /// </summary>
    public static SlowMotionManager Instance { get; private set; }

    private void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

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
        GameManager.Instance?.TriggerSlowMotion(realDuration);
    }
}
