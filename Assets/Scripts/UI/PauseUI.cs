using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the pause menu UI and its button interactions.
/// </summary>
public class PauseUI : BaseUI
{
    [Header("Pause Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button controlsButton;
    [SerializeField] private Button backToMenuButton;

    protected override void Awake()
    {
        base.Awake();
        BindButtons();
    }

    /// <summary>
    /// Binds all button click events to their respective handlers.
    /// </summary>
    private void BindButtons()
    {
        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResumeClick);

        if (optionsButton != null)
            optionsButton.onClick.AddListener(OnOptionsClick);

        if (controlsButton != null)
            controlsButton.onClick.AddListener(OnControlsClick);

        if (backToMenuButton != null)
            backToMenuButton.onClick.AddListener(OnBackToMenuClick);
    }

    /// <summary>
    /// Handles the Resume button click event.
    /// </summary>
    private void OnResumeClick()
    {
        if (PauseManager.Instance != null)
            PauseManager.Instance.ResumeGame();
    }

    /// <summary>
    /// Handles the Options button click event.
    /// </summary>
    private void OnOptionsClick()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.ShowOptions();
    }

    /// <summary>
    /// Handles the Controls button click event.
    /// </summary>
    private void OnControlsClick()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.ShowControls();
    }

    /// <summary>
    /// Handles the Back to Menu button click event.
    /// </summary>
    private void OnBackToMenuClick()
    {
        if (PauseManager.Instance != null)
            PauseManager.Instance.BackToMainMenu();
    }
}
