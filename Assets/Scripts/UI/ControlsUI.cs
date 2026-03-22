using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the controls information panel UI.
/// </summary>
public class ControlsUI : BaseUI
{
    /// <summary>
    /// Enables Escape-close behavior for this panel.
    /// </summary>
    protected override bool CloseOnEscape => true;

    [Header("Controls Elements")]
    [SerializeField] private Button backButton;

    protected override void Awake()
    {
        base.Awake();
        BindButtons();
    }

    /// <summary>
    /// Binds button click events to their handlers.
    /// </summary>
    private void BindButtons()
    {
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClick);
    }

    /// <summary>
    /// Handles the Back button click event.
    /// </summary>
    private void OnBackClick()
    {
        Hide();
        if (UIManager.Instance != null)
            UIManager.Instance.ShowPauseMenu();
    }

    /// <summary>
    /// Handles Escape key behavior by reusing the Back action.
    /// </summary>
    protected override void OnEscapePressed()
    {
        OnBackClick();
    }
}
