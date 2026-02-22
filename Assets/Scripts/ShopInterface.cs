using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using static UIManager;

/// <summary>
/// Manages the shop interface system in the game.
/// </summary>
public class ShopInterface : MonoBehaviour
{
    public static ShopInterface Instance { get; private set; }

    [SerializeField] private GameObject shopPanel;

    private bool isShopOpen = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (shopPanel != null)
            shopPanel.SetActive(false);
    }

    private void Update() {
        HandleCloseShopInput();
    }

    /// <summary>
    /// Processes player input for closing the shop.
    /// Closes the shop when Escape key is pressed while shop is open.
    /// </summary>
    private void HandleCloseShopInput() {
        if (isShopOpen && Keyboard.current.escapeKey.wasPressedThisFrame) {
            CloseShop();
        }
    }

    /// <summary>
    /// Opens the shop interface, pauses the game, and releases the mouse cursor.
    /// Hides the interaction prompt and notifies CharacterInteraction to enter interface mode.
    /// </summary>
    public void OpenShop()
    {
        isShopOpen = true;
        shopPanel.SetActive(true);

        if (Instance != null)
            UIManager.Instance.ToggleInteractionPrompt(false);

        if (CharacterInteraction.Instance != null)
            CharacterInteraction.Instance.SetInterfaceMode(true);
    }

    /// <summary>
    /// Closes the shop interface and returns to gameplay mode.
    /// Notifies CharacterInteraction to resume normal gameplay controls.
    /// </summary>
    public void CloseShop()
    {
        isShopOpen = false;
        shopPanel.SetActive(false);

        if (CharacterInteraction.Instance != null)
            CharacterInteraction.Instance.SetInterfaceMode(false);
    }

    /// <summary>
    /// Returns the current state of the shop interface.
    /// </summary>
    /// <returns>True if the shop is currently open, false otherwise.</returns>
    public bool IsShopOpen() => isShopOpen;
}
