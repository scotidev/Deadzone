using UnityEngine;
using UnityEngine.UI;

/* REFATORAÇÃO:  OnBackClick deveria mostrar o painel de pause? nao tem como fazer voltar para  atela anterior? porque no menu por exemplo nao temos uma tela de pause, nao deveriamos usar a logica contida em CreditsUI por exemplo que faz  /// <summary>
    /// Handles the Back button click event.
    /// </summary>
    private void OnBackClick() {
        Hide();
    } ? */

/// <summary>
/// Manages the controls information panel UI.
/// </summary>
public class ControlsUI : BaseUI {

    #region FIELDS

    [Header("Controls Elements")]
    [SerializeField] private Button backButton;

    /// <summary>
    /// Enables Escape-close behavior for this panel.
    /// </summary>
    protected override bool CloseOnEscape => true;

    #endregion

    #region UNITY

    protected override void Awake() {
        base.Awake();
        BindButtons();
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Binds button click events to their handlers.
    /// </summary>
    private void BindButtons() {
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClick);
    }

    /// <summary>
    /// Handles the Back button click event.
    /// </summary>
    private void OnBackClick() {
        Debug.Log("[ControlsUI] OnBackClick() - ANTES de Hide()");
        Hide();
        Debug.Log("[ControlsUI] OnBackClick() - DEPOIS de Hide(), ANTES de ShowPauseMenu()");
        if (UIManager.Instance != null)
            UIManager.Instance.ShowPauseMenu();
        Debug.Log("[ControlsUI] OnBackClick() - DEPOIS de ShowPauseMenu()");
        UIManager.Instance?.LogVisiblePanels("[ControlsUI] depois de OnBackClick:");
    }

    /// <summary>
    /// Handles Escape key behavior by reusing the Back action.
    /// </summary>
    protected override void OnEscapePressed() {
        Debug.Log("[ControlsUI] OnEscapePressed()");
        OnBackClick();
    }

    #endregion
}
