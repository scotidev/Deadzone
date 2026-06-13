using System.Collections;
using UnityEngine;

// REFATORAÇÃO: Esse script deveria ser um Service do Service Locator? Analise mais profunda necessaria.

/// <summary>
/// Enum that represents every top-level state the game can be in.
/// </summary>
public enum GameState {
    Intro,
    Loading,
    MainMenu,
    Playing,
    Paused,
    Shopping,
    InWave,
    GameOver
}

/// <summary>
/// Persistent singleton that manages global game state and time scale.
/// Attach this component to a persistent GameObject in the loader scene.
/// </summary>
public class GameManager : MonoBehaviour {

    #region STATIC

    /// <summary>Global access point to the single <see cref="GameManager"/> instance.</summary>
    public static GameManager Instance { get; private set; }

    #endregion

    #region SERIALIZED FIELDS

    [Header("Slow Motion Settings")]
    [Tooltip("Time scale factor during slow motion.")]
    [Range(0.01f, 0.9f)]
    [SerializeField] private float slowTimeScale = 0.2f;

    #endregion

    #region FIELDS

    private float _baseTimeScale = 1.0f;

    private Coroutine _slowMotionRoutine;

    #endregion

    #region PROPERTIES

    public GameState State { get; private set; } = GameState.Intro;

    #endregion

    #region UNITY

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(gameObject);
        }
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Transitions the game into <paramref name="newState"/>.
    /// Always call this instead of assigning <see cref="State"/> directly
    /// so that future logging or event hooks can be added here in one place.
    /// </summary>
    /// <param name="newState">The state to transition into.</param>
    public void SetState(GameState newState) {
        State = newState;
    }

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
        Debug.Log($"[GameManager] SetTimeScale({scale}) | _baseTimeScale={_baseTimeScale} | Stack:\n{new System.Diagnostics.StackTrace(true)}");
        _baseTimeScale = scale;

        Time.timeScale = scale;

        Time.fixedDeltaTime = 0.02f * scale;
    }

    /// <summary>
    /// Freezes time by setting <c>Time.timeScale</c> to zero.
    /// Also cancels any active slow motion coroutine first to prevent
    /// it from accidentally resuming the game when its timer expires.
    /// </summary>
    public void PauseTime() {
        CancelSlowMotion();

        Time.timeScale = 0f;
        Time.fixedDeltaTime = 0f;
    }

    /// <summary>
    /// Restores <c>Time.timeScale</c> to <c>_baseTimeScale</c>
    /// (1.0 in normal gameplay unless explicitly changed).
    /// </summary>
    public void ResumeTime() {
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

    /// <summary>
    /// Resets all session data so a new game starts fresh.
    /// Called before transitioning to Menu / SelectMap when the player
    /// returns to the menu or starts a new game.
    /// </summary>
    public static void ResetGameSession() {
        Debug.Log("[GameManager] Resetting game session data...");

        PlayerProgress.Instance?.ResetProgress();

        EconomyManager.Instance?.ResetCurrency();

        PenguinMode.Reset();

        Debug.Log("[GameManager] Game session has been reset.");
    }

    /// <summary>
    /// Coroutine that runs the full slow motion lifecycle:
    /// applies the slow time scale, waits in real time, then restores
    /// the base time scale.
    /// </summary>
    /// <param name="realDuration">Duration measured in real (unscaled) seconds.</param>
    private IEnumerator SlowMotionRoutine(float realDuration) {
        Time.timeScale = slowTimeScale;

        // Adjust physics tick rate proportionally.
        Time.fixedDeltaTime = 0.02f * slowTimeScale;

        yield return new WaitForSecondsRealtime(realDuration);

        Time.timeScale = _baseTimeScale;
        Time.fixedDeltaTime = 0.02f * _baseTimeScale;

        _slowMotionRoutine = null;
    }

    #endregion
}
