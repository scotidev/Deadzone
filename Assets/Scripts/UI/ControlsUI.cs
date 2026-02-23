using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the controls information panel UI.
/// </summary>
public class ControlsUI : BaseUI
{
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
}
