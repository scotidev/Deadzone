using System.Collections;
using UnityEngine;

// ==============================================================
//  DESIGN PATTERN: SINGLETON
// ==============================================================
//  The Singleton pattern ensures that only ONE instance of a class
//  exists at any time. The static "Instance" property gives every
//  script in the project a global access point without requiring
//  Inspector drag-and-drop references.
//
//  Usage from any other script:
//      GameManager.Instance?.SetState(GameState.Playing);
//
//  The "?" is the null-conditional operator: if Instance is null
//  (manager not loaded yet), the call is silently skipped instead
//  of throwing a NullReferenceException.
// ==============================================================

// ==============================================================
//  DESIGN PRINCIPLE: SINGLE SOURCE OF TRUTH
// ==============================================================
//  Before this refactor, Time.timeScale was written from multiple
//  places (PauseManager, SlowMotionManager, TimeHandler).
//  That makes it impossible to reason about the time state from
//  one single location — a classic maintenance problem.
//
//  After this refactor, GameManager is the ONLY class allowed to
//  write to Time.timeScale. All other systems call its public
//  methods. This is the "Single Source of Truth" principle.
// ==============================================================

/// <summary>
/// Enum that represents every top-level state the game can be in.
/// Other systems read <see cref="GameManager.State"/> to decide
/// what actions are allowed (e.g. enemies should only move in
/// <see cref="Playing"/> or <see cref="InWave"/>).
/// </summary>
public enum GameState {
    /// <summary>Initial state while the loading screen is active.</summary>
    Loader,
    /// <summary>Player is on the main menu.</summary>
    MainMenu,
    /// <summary>Normal gameplay between waves.</summary>
    Playing,
    /// <summary>Game is paused — time is frozen, pause menu is visible.</summary>
    Paused,
    /// <summary>Player has the shop interface open.</summary>
    Shopping,
    /// <summary>A wave of enemies is actively running.</summary>
    InWave
}

/// <summary>
/// Persistent singleton that owns two responsibilities:
/// <list type="number">
///   <item><description>
///     Tracking the global <see cref="GameState"/> so other systems can
///     query what is allowed at any moment.
///   </description></item>
///   <item><description>
///     Being the <b>single source of truth</b> for <c>Time.timeScale</c>.
///     No other class may write to <c>Time.timeScale</c> directly;
///     they must call the methods provided here.
///   </description></item>
/// </list>
/// Attach this component to a persistent GameObject in the bootstrap/loader scene.
/// </summary>
public class GameManager : MonoBehaviour {

    // ==============================================================
    //  SINGLETON FIELDS
    // ==============================================================
    //  "public static" → readable by any script anywhere.
    //  "private set"   → writable only inside GameManager itself.
    //  The value is assigned once in Awake() and never changed again.
    // ==============================================================

    /// <summary>Global access point to the single <see cref="GameManager"/> instance.</summary>
    public static GameManager Instance { get; private set; }

    /// <summary>
    /// The current high-level state of the game.
    /// Read-only from outside; change it only via <see cref="SetState"/>.
    /// </summary>
    public GameState State { get; private set; } = GameState.Loader;

    // ==============================================================
    //  TIME SCALE FIELDS
    // ==============================================================
    //  [SerializeField] exposes a private field in the Unity Inspector.
    //  [Range(min, max)] renders it as a slider, preventing bad values.
    //
    //  _baseTimeScale stores the "intended" game speed (normally 1.0).
    //  It is updated ONLY when SetTimeScale() is called — never when
    //  applying slow motion or pausing. That way, after a pause or
    //  slow motion ends, we always restore the correct base speed
    //  without hard-coding the value 1f everywhere.
    // ==============================================================

    [Header("Slow Motion Settings")]
    [Tooltip("Time scale factor during slow motion. 0.2 = 20% of normal speed.")]
    [Range(0.01f, 0.9f)]
    [SerializeField] private float slowTimeScale = 0.2f;

    /// <summary>
    /// The intended game speed saved by <see cref="SetTimeScale"/>.
    /// Restored by <see cref="ResumeTime"/> and at the end of <see cref="SlowMotionRoutine"/>.
    /// Defaults to 1.0 (normal speed).
    /// </summary>
    private float _baseTimeScale = 1.0f;

    /// <summary>
    /// Reference to the currently running slow motion coroutine, or
    /// <c>null</c> when no slow motion is active.
    /// Storing this reference lets us cancel and restart the effect
    /// mid-flight when a chain explosion fires a second trigger.
    /// </summary>
    private Coroutine _slowMotionRoutine;

    // ==============================================================
    //  UNITY LIFECYCLE
    // ==============================================================

    private void Awake() {
        // Classic Singleton guard:
        // If an instance already exists (persisted from a previous scene
        // via DontDestroyOnLoad), this duplicate destroys itself.
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Survives scene transitions.
        }
        else {
            Destroy(gameObject);
        }
    }

    // ==============================================================
    //  GAME STATE API
    // ==============================================================

    /// <summary>
    /// Transitions the game into <paramref name="newState"/>.
    /// Always call this instead of assigning <see cref="State"/> directly
    /// so that future logging or event hooks can be added here in one place.
    /// </summary>
    /// <param name="newState">The state to transition into.</param>
    public void SetState(GameState newState) {
        State = newState;
    }

    // ==============================================================
    //  TIME SCALE API  —  Single Source of Truth
    // ==============================================================
    //  RULE: Time.timeScale must ONLY be written inside this region.
    //  Every other script in the project calls these methods instead.
    //
    //  WHY also adjust fixedDeltaTime?
    //  FixedUpdate (used by the physics engine) runs on a fixed
    //  interval defined by Time.fixedDeltaTime. When timeScale
    //  changes, that interval must scale proportionally so physics
    //  still runs at the correct logical rate (50 ticks per game-second).
    //  Formula:  fixedDeltaTime = 0.02f * timeScale
    //  (0.02f = 1/50, the default Unity physics tick rate)
    // ==============================================================

    /// <summary>
    /// Sets <c>Time.timeScale</c> to <paramref name="scale"/> and adjusts
    /// <c>Time.fixedDeltaTime</c> proportionally to keep physics consistent.
    /// Saves the value as <c>_baseTimeScale</c> so <see cref="ResumeTime"/>
    /// can restore it later.
    /// </summary>
    /// <param name="scale">
    /// The desired time multiplier. 1.0 = normal speed, 0.5 = half speed.
    /// </param>
    public void SetTimeScale(float scale) {
        // Persist this as the "ground truth" speed so ResumeTime knows
        // what to restore after a pause or slow motion effect finishes.
        _baseTimeScale = scale;

        Time.timeScale = scale;

        // Without this adjustment, Rigidbodies behave incorrectly
        // during time manipulation (physics runs at wrong intervals).
        Time.fixedDeltaTime = 0.02f * scale;
    }

    /// <summary>
    /// Freezes time by setting <c>Time.timeScale</c> to zero.
    /// Also cancels any active slow motion coroutine first to prevent
    /// it from accidentally resuming the game when its timer expires.
    /// </summary>
    public void PauseTime() {
        // Cancel slow motion BEFORE freezing time.
        // If we skipped this, the coroutine would keep counting down
        // with WaitForSecondsRealtime (real time keeps ticking even at
        // timeScale = 0) and would call SetTimeScale(_baseTimeScale)
        // the moment it finished — unpausing the game behind the
        // player's back.
        CancelSlowMotion();

        Time.timeScale = 0f;
        Time.fixedDeltaTime = 0f;
    }

    /// <summary>
    /// Restores <c>Time.timeScale</c> to <c>_baseTimeScale</c>
    /// (1.0 in normal gameplay unless explicitly changed).
    /// </summary>
    public void ResumeTime() {
        // SetTimeScale handles both Time.timeScale and fixedDeltaTime,
        // so we never duplicate that logic here.
        SetTimeScale(_baseTimeScale);
    }

    /// <summary>
    /// Starts a slow motion effect lasting <paramref name="realDuration"/>
    /// seconds of <b>real (wall-clock) time</b>, unaffected by the reduced
    /// time scale. If slow motion is already running, the timer resets,
    /// which lets chain explosions naturally extend the effect.
    /// </summary>
    /// <param name="realDuration">
    /// How long the slow motion lasts in real seconds (not game seconds).
    /// </param>
    public void TriggerSlowMotion(float realDuration) {
        // Stop any in-progress slow motion coroutine before starting a
        // new one — this resets the countdown from zero.
        if (_slowMotionRoutine != null)
            StopCoroutine(_slowMotionRoutine);

        _slowMotionRoutine = StartCoroutine(SlowMotionRoutine(realDuration));
    }

    /// <summary>
    /// Stops the active slow motion coroutine without touching <c>Time.timeScale</c>.
    /// Called internally by <see cref="PauseTime"/> to prevent post-pause side effects.
    /// </summary>
    public void CancelSlowMotion() {
        if (_slowMotionRoutine == null) return;

        StopCoroutine(_slowMotionRoutine);
        _slowMotionRoutine = null;
    }

    // ==============================================================
    //  COROUTINE: SLOW MOTION LIFECYCLE
    // ==============================================================
    //  A Coroutine is a method that can suspend its own execution at
    //  a "yield return" statement and resume later — without blocking
    //  the main game loop or freezing the screen.
    //
    //  IEnumerator is the required return type. The C# compiler uses
    //  it internally to know that this method can be paused.
    //
    //  Step-by-step flow of this coroutine:
    //    1. Apply slowTimeScale    → slow motion begins.
    //    2. yield WaitForSecondsRealtime → wait in REAL wall-clock time.
    //    3. Restore _baseTimeScale → slow motion ends.
    // ==============================================================

    /// <summary>
    /// Coroutine that runs the full slow motion lifecycle:
    /// applies the slow time scale, waits in real time, then restores
    /// the base time scale.
    /// </summary>
    /// <param name="realDuration">Duration measured in real (unscaled) seconds.</param>
    private IEnumerator SlowMotionRoutine(float realDuration) {
        // Step 1 — Apply the slow-motion factor set in the Inspector.
        Time.timeScale = slowTimeScale;

        // Adjust physics tick rate proportionally (see SetTimeScale for details).
        Time.fixedDeltaTime = 0.02f * slowTimeScale;

        // Step 2 — Wait in REAL time.
        //
        // WHY WaitForSecondsRealtime instead of WaitForSeconds?
        //
        //   WaitForSeconds(t)         respects timeScale.
        //   → With timeScale = 0.2, waiting "1 game-second" takes 5 real
        //     seconds. The slow motion would last far too long.
        //
        //   WaitForSecondsRealtime(t) reads the system clock directly.
        //   → Always waits exactly t real-world seconds, regardless of
        //     how low timeScale is. This is exactly what we want.
        yield return new WaitForSecondsRealtime(realDuration);

        // Step 3 — Restore the speed that was active before slow motion.
        // We use _baseTimeScale (not the hard-coded 1f) so this works
        // correctly even if someone intentionally set a custom base speed
        // via SetTimeScale() before the explosion occurred.
        Time.timeScale = _baseTimeScale;
        Time.fixedDeltaTime = 0.02f * _baseTimeScale;

        // Clear the reference — signals that no slow motion is running.
        _slowMotionRoutine = null;
    }
}
