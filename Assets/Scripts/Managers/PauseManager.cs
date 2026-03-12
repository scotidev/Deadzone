using UnityEngine;
using UnityEngine.InputSystem;

// ==============================================================
//  RESPONSIBILITY OF THIS CLASS
// ==============================================================
//  PauseManager owns the GAME LOGIC of pausing:
//    - detecting the Escape key
//    - toggling the isPaused flag
//    - showing/hiding the pause menu UI
//    - locking/unlocking the cursor
//
//  It does NOT own time scale manipulation anymore.
//  For that it calls GameManager.PauseTime() / ResumeTime(),
//  keeping the Single Source of Truth principle intact.
// ==============================================================

/// <summary>
/// Singleton that manages the game's pause state and related behaviour:
/// UI visibility, cursor locking, input interception, and delegation
/// of time scale changes to <see cref="GameManager"/>.
/// </summary>
public class PauseManager : MonoBehaviour {

    /// <summary>Global access point to the single <see cref="PauseManager"/> instance.</summary>
    public static PauseManager Instance { get; private set; }

    // Tracks whether the game is currently paused so TogglePause
    // knows which direction to go.
    private bool isPaused = false;

    private void Awake() {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start() {
        // When the gameplay scene finishes loading, tell GameManager
        // that we are now in normal play mode.
        GameManager.Instance?.SetState(GameState.Playing);
    }

    private void Update() {
        HandlePauseInput();
    }

    // ==============================================================
    //  INPUT
    // ==============================================================

    /// <summary>
    /// Reads keyboard input each frame and toggles or closes menus
    /// when the Escape key is pressed.
    /// </summary>
    private void HandlePauseInput() {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            return;

        // If the shop is open, Escape closes the shop instead of pausing.
        if (GameManager.Instance?.State == GameState.Shopping) {
            ShopManager.Instance?.CloseShop();
            return;
        }

        TogglePause();
    }

    // ==============================================================
    //  PAUSE TOGGLE
    // ==============================================================

    /// <summary>
    /// Toggles between paused and unpaused states based on
    /// the current value of <see cref="isPaused"/>.
    /// </summary>
    private void TogglePause() {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    // ==============================================================
    //  PUBLIC PAUSE / RESUME
    // ==============================================================
    //  Notice that neither PauseGame nor ResumeGame write to
    //  Time.timeScale directly. They call GameManager methods instead.
    //
    //  Why does this matter?
    //  If we wrote Time.timeScale = 0f here AND in SlowMotionManager,
    //  we would have two systems fighting over the same value with no
    //  coordination. By routing everything through GameManager we get:
    //    • a single place to debug time-related bugs
    //    • automatic cancellation of slow motion on pause
    //    • correct restoration of the base speed on resume
    // ==============================================================

    /// <summary>
    /// Pauses the game: freezes time via <see cref="GameManager.PauseTime"/>,
    /// shows the pause menu, and unlocks the cursor.
    /// </summary>
    public void PauseGame() {
        isPaused = true;
        GameManager.Instance?.SetState(GameState.Paused);

        if (CharacterInteraction.Instance != null)
            CharacterInteraction.Instance.SetInterfaceMode(true);

        SetCursorState(true);

        if (UIManager.Instance != null)
            UIManager.Instance.ShowPauseMenu();

        // Delegate time freezing to GameManager.
        // This also cancels any active slow motion coroutine so it cannot
        // accidentally unfreeze the game when its real-time timer expires.
        GameManager.Instance?.PauseTime();
    }

    /// <summary>
    /// Resumes the game: restores time via <see cref="GameManager.ResumeTime"/>,
    /// hides all menus, and locks the cursor back to the viewport.
    /// </summary>
    public void ResumeGame() {
        // Restore Time.timeScale to the base speed (1.0 in normal gameplay).
        // GameManager.ResumeTime() internally calls SetTimeScale(_baseTimeScale),
        // which also fixes Time.fixedDeltaTime in one step.
        GameManager.Instance?.ResumeTime();
        isPaused = false;
        GameManager.Instance?.SetState(GameState.Playing);

        if (UIManager.Instance != null)
            UIManager.Instance.HideAllPanels();

        SetCursorState(false);

        if (CharacterInteraction.Instance != null)
            CharacterInteraction.Instance.SetInterfaceMode(false);
    }

    /// <summary>
    /// Unloads the gameplay scene and returns to the main menu.
    /// Restores time before loading so the menu runs at normal speed.
    /// </summary>
    public void BackToMainMenu() {
        // Always restore time before a scene transition.
        // If the player opened the pause menu while slow motion was active,
        // GameManager already cancelled it inside PauseTime(). Calling
        // ResumeTime() here guarantees a clean state for the menu scene.
        GameManager.Instance?.ResumeTime();
        GameManager.Instance?.SetState(GameState.MainMenu);
        SceneLoader.Instance.LoadMenu();
    }

    // ==============================================================
    //  HELPERS
    // ==============================================================

    /// <summary>
    /// Sets cursor visibility and lock state together so they never
    /// get out of sync.
    /// </summary>
    /// <param name="visible">
    /// <c>true</c> to show and free the cursor (menus);
    /// <c>false</c> to hide and lock it to the viewport (gameplay).
    /// </param>
    private void SetCursorState(bool visible) {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }

    /// <summary>Returns <c>true</c> if the game is currently paused.</summary>
    public bool IsPaused() => isPaused;
}
