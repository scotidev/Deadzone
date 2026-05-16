using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the pause menu UI and its button interactions.
/// </summary>
public class PauseUI : BaseUI {

    #region SERIALIZED FIELDS

    [Header("Pause Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button controlsButton;
    [SerializeField] private Button backToMenuButton;

    #endregion

    #region UNITY

    protected override void Awake() {
        base.Awake();
        BindButtons();
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Binds all button click events to their respective handlers.
    /// </summary>
    private void BindButtons() {
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
    private void OnResumeClick() {
        Debug.Log("[PauseUI] Botão Resume clicado");
        if (PauseManager.Instance != null)
            PauseManager.Instance.ResumeGame();
        UIManager.Instance?.LogVisiblePanels("[PauseUI] depois de Resume:");
    }

    /// <summary>
    /// Handles the Options button click event.
    /// </summary>
    private void OnOptionsClick() {
        Debug.Log("[PauseUI] Botão Options clicado");
        if (UIManager.Instance != null)
            UIManager.Instance.ShowOptions();
        UIManager.Instance?.LogVisiblePanels("[PauseUI] depois de Options:");
    }

    /// <summary>
    /// Handles the Controls button click event.
    /// </summary>
    private void OnControlsClick() {
        Debug.Log("[PauseUI] Botão Controls clicado");
        if (UIManager.Instance != null)
            UIManager.Instance.ShowControls();
        UIManager.Instance?.LogVisiblePanels("[PauseUI] depois de Controls:");
    }

    /// <summary>
    /// Handles the Back to Menu button click event.
    /// </summary>
    private void OnBackToMenuClick() {
        if (PauseManager.Instance != null)
            PauseManager.Instance.BackToMainMenu();
    }

    #endregion
}
