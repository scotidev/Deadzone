using UnityEngine;

/// <summary>
/// Central coordinator for UI. Manages all game panels in the game.
/// Acts as a mediator between game systems and UI components.
/// </summary>
public class UIManager : MonoBehaviour {
    /// <summary>Global access point to the single <see cref="UIManager"/> instance.</summary>
    public static UIManager Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private PauseUI pauseUI;
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private OptionsUI optionsUI;
    [SerializeField] private ControlsUI controlsUI;
    [SerializeField] private InteractionPromptUI interactionPromptUI;

    [Header("HUD")]
    [Tooltip("Wave information HUD. NOT hidden by HideAllPanels.")]
    [SerializeField] private WaveUI waveUI;

    private void Awake() {
        InitializeSingleton();
    }

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
        if (shopUI != null)
            shopUI.Show();
    }

    /// <summary>
    /// Exhibits the options panel (keeping the pause panel hidden)
    /// </summary>
    public void ShowOptions() {
        if (pauseUI != null)
            pauseUI.Hide();

        if (optionsUI != null)
            optionsUI.Show();
    }

    /// <summary>
    /// Exhibits the controls panel (keeping the pause panel hidden)
    /// </summary>
    public void ShowControls() {
        if (pauseUI != null)
            pauseUI.Hide();

        if (controlsUI != null)
            controlsUI.Show();
    }

    /// <summary>
    /// Hides all panels except WaveUI. 
    /// Hides all overlays.
    /// </summary>
    public void HideAllPanels() {
        if (pauseUI != null) pauseUI.Hide();
        if (shopUI != null) shopUI.Hide();
        if (optionsUI != null) optionsUI.Hide();
        if (controlsUI != null) controlsUI.Hide();
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
}
