using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Base class for all UI panels in the game.
/// Provides common functionality for showing and hiding panels.
/// </summary>
public abstract class BaseUI : MonoBehaviour {

    #region FIELDS

    [SerializeField] protected GameObject panel;

    private bool _showCalledBeforeStart = false;

    #endregion

    #region PROPERTIES

    /// <summary>
    /// Gets whether this panel should close when Escape is pressed.
    /// </summary>
    protected virtual bool CloseOnEscape => false;

    #endregion

    #region METHODS

    /// <summary>
    /// Shows the UI panel.
    /// </summary>
    public virtual void Show() {
        _showCalledBeforeStart = true;
        if (panel != null) {
            panel.SetActive(true);

            Canvas.ForceUpdateCanvases();
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(panel.GetComponent<RectTransform>());
        }
    }

    /// <summary>
    /// Hides the UI panel.
    /// </summary>
    public virtual void Hide() {
        if (panel != null)
            panel.SetActive(false);
    }

    /// <summary>
    /// Checks if the panel is currently visible.
    /// </summary>
    /// <returns>True if panel is active, false otherwise.</returns>
    public bool IsVisible() {
        return panel != null && panel.activeSelf;
    }

    /// <summary>
    /// Initializes the UI panel on awake.
    /// Only hides the panel if it's NOT the same GameObject as this script.
    /// </summary>
    protected virtual void Awake() {
    }

    /// <summary>
    /// Hides the panel on Start to ensure it's hidden by default.
    /// </summary>
    protected virtual void Start() {
        if (!_showCalledBeforeStart && panel != null)
            panel.SetActive(false);
    }

    /// <summary>
    /// Handles panel keyboard shortcuts.
    /// </summary>
    protected virtual void Update() {
        HandleEscapeClose();
    }

    /// <summary>
    /// Hides the panel when Escape is pressed.
    /// </summary>
    private void HandleEscapeClose() {
        if (!CloseOnEscape || !IsVisible())
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) {
            OnEscapePressed();
        }
    }

    /// <summary>
    /// Called when Escape is pressed while this panel is visible.
    /// </summary>
    protected virtual void OnEscapePressed() {
        Hide();
    }

    #endregion
}
