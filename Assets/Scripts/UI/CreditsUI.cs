using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the credits panel UI.
/// </summary>
public class CreditsUI : BaseUI {
    [Header("Credits Elements")]
    [SerializeField] private Button backButton;

    protected override void Awake() {
        base.Awake();
        BindButtons();
    }

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
        Hide();
    }
}
