using UnityEngine;
using InfimaGames.LowPolyShooterPack;

/// <summary>
/// Manages the shop interface system in the game.
/// </summary>
public class ShopManager : MonoBehaviour {
    /// <summary>Global access point to the single <see cref="ShopManager"/> instance.</summary>
    public static ShopManager Instance { get; private set; }

    private bool isShopOpen = false;
    [SerializeField] private Character playerCharacter;

    private void Awake() {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        ResolvePlayerCharacter();
    }

    /// <summary>
    /// Ensures a valid reference to the player's character component.
    /// </summary>
    private void ResolvePlayerCharacter() {
        if (playerCharacter != null)
            return;

        playerCharacter = FindObjectOfType<Character>();
    }

    /// <summary>
    /// Opens the shop interface.
    /// Hides the interaction prompt and puts the player character in interface mode.
    /// </summary>
    public void OpenShop() {
        ResolvePlayerCharacter();
        isShopOpen = true;
        GameManager.Instance?.SetState(GameState.Shopping);

        if (UIManager.Instance != null) {
            UIManager.Instance.ShowShop();
            UIManager.Instance.HideInteractionPrompt();
        }

        if (playerCharacter != null)
            playerCharacter.SetInterfaceMode(true);

        SetCursorState(true);
    }

    /// <summary>
    /// Closes the shop interface and returns to gameplay mode.
    /// Restores normal player character controls.
    /// </summary>
    public void CloseShop() {
        ResolvePlayerCharacter();
        isShopOpen = false;
        GameManager.Instance?.SetState(GameState.Playing);

        if (UIManager.Instance != null)
            UIManager.Instance.HideAllPanels();

        if (playerCharacter != null)
            playerCharacter.SetInterfaceMode(false);

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
