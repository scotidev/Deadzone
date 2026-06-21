using UnityEngine;
using Deadzone.UI;

/// <summary>
/// Central coordinator for UI. Manages all game panels in the game.
/// Acts as a mediator between game systems and UI components.
/// </summary>
public class UIManager : MonoBehaviour {

    #region STATIC

    /// <summary>Global access point to the single <see cref="UIManager"/> instance.</summary>
    public static UIManager Instance { get; private set; }

    #endregion

    #region SERIALIZED FIELDS

    [Header("UI Panels")]
    [SerializeField] private PauseUI pauseUI;
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private OptionsUI optionsUI;
    [SerializeField] private ControlsUI controlsUI;
    [SerializeField] private InteractionPromptUI interactionPromptUI;
    [SerializeField] private GameOverUI gameOverUI;

    [Header("HUD")]
    [SerializeField] private WaveUI waveUI;
    [SerializeField] private GameObject hudRoot;

    [Header("Visual Feedback")]
    [SerializeField] private HealFeedbackUI healFeedbackUI;

    #endregion

    #region UNITY

    private void Awake() {
        InitializeSingleton();
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Ensures only one instance of UIManager exists.
    /// </summary>
    private void InitializeSingleton() {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// Exhibits the pause menu and hides all other panels.
    /// </summary>
    public void ShowPauseMenu() {
        HideAllPanels();
        if (pauseUI != null)
            pauseUI.Show();
    }

    /// <summary>
    /// Exhibits the shop panel and hide all other panels.
    /// </summary>
    public void ShowShop() {
        HideAllPanels();
        if (hudRoot != null) hudRoot.SetActive(false);
        if (shopUI != null)
            shopUI.Show();
    }

    /// <summary>
    /// Exhibits the options panel.
    /// </summary>
    public void ShowOptions() {
        if (pauseUI != null)
            pauseUI.Hide();

        if (optionsUI != null)
            optionsUI.Show();
    }

    /// <summary>
    /// Exhibits the controls panel.
    /// </summary>
    public void ShowControls() {
        if (pauseUI != null)
            pauseUI.Hide();

        if (controlsUI != null)
            controlsUI.Show();
    }

    /// <summary>
    /// Hides all panels and shows the HUD root.
    /// </summary>
    public void HideAllPanels() {
        if (pauseUI != null) pauseUI.Hide();
        if (shopUI != null) shopUI.Hide();
        if (optionsUI != null) optionsUI.Hide();
        if (controlsUI != null) controlsUI.Hide();
        if (hudRoot != null) hudRoot.SetActive(true);
    }

    /// <summary>
    /// Exhibits the Game Over panel and hides all panels and HUD.
    /// Called by GameOverManager when the player dies.
    /// </summary>
    public void ShowGameOver(int wave) {
        HideAllPanels();
        if (hudRoot != null) hudRoot.SetActive(false);
        if (gameOverUI != null) {
            gameOverUI.SetWaveNumber(wave);
            gameOverUI.Show();
        }
    }

    /// <summary>
    /// Hides the Game Over panel.
    /// </summary>
    public void HideGameOver() {
        if (gameOverUI != null)
            gameOverUI.Hide();
    }

    /// <summary>
    /// Exhibits the wave HUD.
    /// </summary>
    public void ShowWaveHUD() {
        if (waveUI != null)
            waveUI.Show();
    }

    /// <summary>
    /// Hides the wave HUD.
    /// </summary>
    public void HideWaveHUD() {
        if (waveUI != null)
            waveUI.Hide();
    }

    /// <summary>
    /// Exhibits the interaction prompt with the specified message.
    /// Called by PlayerInteraction when detecting an Interactable.
    /// </summary>
    public void ShowInteractionPrompt(string message) {
        if (interactionPromptUI != null)
            interactionPromptUI.Show(message);
    }

    /// <summary>
    /// Hides the Interaction Prompt.
    /// </summary>
    public void HideInteractionPrompt() {
        if (interactionPromptUI != null)
            interactionPromptUI.Hide();
    }

    /// <summary>
    /// Switches the interaction prompt visibility.
    /// Legacy method kept for compatibility.
    /// </summary>
    public void ToggleInteractionPrompt(bool show, string message = "") {
        if (show)
            ShowInteractionPrompt(message);
        else
            HideInteractionPrompt();
    }

    /// <summary>
    /// Checks if the interaction prompt is currently visible on screen.
    /// </summary>
    public bool IsInteractionPromptActive() {
        return interactionPromptUI != null && interactionPromptUI.IsVisible();
    }

    /// <summary>
    /// Shows the heal visual feedback effect on screen edges.
    /// Called by Medkit when successfully healing the player.
    /// </summary>
    /// <param name="duration">How long the effect lasts in seconds.</param>
    public void ShowHealFeedback(float duration) {
        if (healFeedbackUI != null) {
            healFeedbackUI.Show(duration);
        }
    }

    #endregion

}
