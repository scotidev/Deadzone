using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages the game pause state and related functionality.
/// </summary>
public class PauseManager : MonoBehaviour {
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
    /// Handles the pause input from the player.
    /// Toggles pause state when Escape key is pressed.
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
    /// Toggles between paused and unpaused states.
    /// </summary>
    private void TogglePause() {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    /// <summary>
    /// Pauses the game and shows the pause menu.
    /// </summary>
    public void PauseGame() {
        isPaused = true;
        GameManager.Instance?.SetState(GameState.Paused);

        if (CharacterInteraction.Instance != null)
            CharacterInteraction.Instance.SetInterfaceMode(true);

        SetCursorState(true);

        if (UIManager.Instance != null)
            UIManager.Instance.ShowPauseMenu();

        Time.timeScale = 0f;
    }

    /// <summary>
    /// Resumes the game and hides the pause menu.
    /// Restores Time.timeScale to 1.
    /// </summary>
    public void ResumeGame() {
        Time.timeScale = 1f;
        isPaused = false;
        GameManager.Instance?.SetState(GameState.Playing);

        if (UIManager.Instance != null)
            UIManager.Instance.HideAllPanels();

        SetCursorState(false);

        if (CharacterInteraction.Instance != null)
            CharacterInteraction.Instance.SetInterfaceMode(false);
    }

    /// <summary>
    /// Returns to the main menu scene.
    /// </summary>
    public void BackToMainMenu() {
        Time.timeScale = 1f;
        GameManager.Instance?.SetState(GameState.MainMenu);
        SceneLoader.Instance.LoadMenu();
    }

    /// <summary>
    /// Sets the cursor lock state and visibility.
    /// </summary>
    /// <param name="visible">True to show cursor, false to hide and lock.</param>
    private void SetCursorState(bool visible) {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }

    /// <summary>
    /// Returns the current pause state.
    /// </summary>
    /// <returns>True if game is paused, false otherwise.</returns>
    public bool IsPaused() => isPaused;
}
