using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Singleton that manages the game's pause state and related behaviour:
/// UI visibility, cursor locking, input interception, and delegation
/// of time scale changes to <see cref="GameManager"/>.
/// </summary>
public class PauseManager : MonoBehaviour {

    /// <summary>Global access point to the single <see cref="PauseManager"/> instance.</summary>
    public static PauseManager Instance { get; private set; }

    private bool isPaused = false;

    private void Awake() {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start() {
        GameManager.Instance?.SetState(GameState.Playing);
    }

    private void Update() {
        HandlePauseInput();
    }

    /// <summary>
    /// Reads keyboard input each frame and toggles or closes menus
    /// when the Escape key is pressed.
    /// </summary>
    private void HandlePauseInput() {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            return;

        if (GameManager.Instance?.State == GameState.Shopping) {
            ShopManager.Instance?.CloseShop();
            return;
        }

        TogglePause();
    }

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

        GameManager.Instance?.PauseTime();
    }

    /// <summary>
    /// Resumes the game: restores time via <see cref="GameManager.ResumeTime"/>,
    /// hides all menus, and locks the cursor back to the viewport.
    /// </summary>
    public void ResumeGame() {
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
        GameManager.Instance?.ResumeTime();
        GameManager.Instance?.SetState(GameState.MainMenu);
        SceneLoader.Instance.LoadMenu();
    }

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
