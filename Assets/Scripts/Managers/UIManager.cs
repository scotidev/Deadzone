using UnityEngine;

/// <summary>
/// Central UI coordinator that manages all UI panels in the game.
/// Acts as a mediator between different UI components.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Components")]
    [SerializeField] private PauseUI pauseUI;
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private OptionsUI optionsUI;
    [SerializeField] private ControlsUI controlsUI;
    [SerializeField] private InteractionPromptUI interactionPromptUI;

    private void Awake()
    {
        InitializeSingleton();
    }

    /// <summary>
    /// Initializes the singleton instance.
    /// </summary>
    private void InitializeSingleton()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// Shows the pause menu and hides all other panels.
    /// </summary>
    public void ShowPauseMenu()
    {
        HideAllPanels();
        if (pauseUI != null)
            pauseUI.Show();
    }

    /// <summary>
    /// Shows the shop UI and hides all other panels.
    /// </summary>
    public void ShowShop()
    {
        HideAllPanels();
        if (shopUI != null)
            shopUI.Show();
    }

    /// <summary>
    /// Shows the options panel and hides all other panels except pause.
    /// </summary>
    public void ShowOptions()
    {
        if (pauseUI != null)
            pauseUI.Hide();

        if (optionsUI != null)
            optionsUI.Show();
    }

    /// <summary>
    /// Shows the controls panel and hides all other panels except pause.
    /// </summary>
    public void ShowControls()
    {
        if (pauseUI != null)
            pauseUI.Hide();

        if (controlsUI != null)
            controlsUI.Show();
    }

    /// <summary>
    /// Hides all UI panels.
    /// </summary>
    public void HideAllPanels()
    {
        if (pauseUI != null) pauseUI.Hide();
        if (shopUI != null) shopUI.Hide();
        if (optionsUI != null) optionsUI.Hide();
        if (controlsUI != null) controlsUI.Hide();
    }

    /// <summary>
    /// Shows the interaction prompt with a message.
    /// </summary>
    /// <param name="message">The message to display.</param>
    public void ShowInteractionPrompt(string message)
    {
        if (interactionPromptUI != null)
            interactionPromptUI.Show(message);
    }

    /// <summary>
    /// Hides the interaction prompt.
    /// </summary>
    public void HideInteractionPrompt()
    {
        if (interactionPromptUI != null)
            interactionPromptUI.Hide();
    }

    /// <summary>
    /// Toggles the interaction prompt visibility.
    /// Legacy method for backward compatibility.
    /// </summary>
    /// <param name="show">True to show, false to hide.</param>
    /// <param name="message">The message to display.</param>
    public void ToggleInteractionPrompt(bool show, string message = "")
    {
        if (show)
            ShowInteractionPrompt(message);
        else
            HideInteractionPrompt();
    }

    }
