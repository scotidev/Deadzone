using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages the shop interface system in the game.
/// </summary>
public class ShopManager : MonoBehaviour {
    public static ShopManager Instance { get; private set; }

    private bool isShopOpen = false;

    private void Awake() {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// Opens the shop interface
    /// Hides the interaction prompt and notifies CharacterInteraction to enter interface mode.
    /// </summary>
    public void OpenShop() {
        isShopOpen = true;
        GameManager.Instance?.SetState(GameState.Shopping);

        if (UIManager.Instance != null) {
            UIManager.Instance.ShowShop();
            UIManager.Instance.HideInteractionPrompt();
        }

        if (CharacterInteraction.Instance != null)
            CharacterInteraction.Instance.SetInterfaceMode(true);

        SetCursorState(true);
    }

    /// <summary>
    /// Closes the shop interface and returns to gameplay mode.
    /// Notifies CharacterInteraction to resume normal gameplay controls.
    /// </summary>
    public void CloseShop() {
        isShopOpen = false;
        GameManager.Instance?.SetState(GameState.Playing);

        if (UIManager.Instance != null)
            UIManager.Instance.HideAllPanels();

        if (CharacterInteraction.Instance != null)
            CharacterInteraction.Instance.SetInterfaceMode(false);

        SetCursorState(false);
    }

    /// <summary>
    /// Sets the cursor lock state and visibility.
    /// </summary>
    /// <param name="visible">True to show cursor, false to hide and lock.</param>
    private void SetCursorState(bool visible) {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }

    /// <summary>
    /// Returns the current state of the shop interface.
    /// </summary>
    /// <returns>True if the shop is currently open, false otherwise.</returns>
    public bool IsShopOpen() => isShopOpen;
}
