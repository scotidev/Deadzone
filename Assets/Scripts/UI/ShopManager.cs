using InfimaGames.LowPolyShooterPack;
using UnityEngine;

// REFATORAÇÃO: esse script deve eu nao deve ser um Serviço do ServiceLocator? analise necessaria
// REFATORAÇÃO: a lógica de desbloquear items, atualizar o inventario etc deve ser feita aqui? atualmente ou ela está em ShopItemCard ou ShopUI. analise necessaria, precisamos dar a responsabilidade de gerenciar o sistema de loja para um único script, evitando que a lógica fique espalhada por vários componentes, o que pode dificultar a manutenção e evolução do código.

/// <summary>
/// Manages the shop interface system in the game.
/// </summary>
public class ShopManager : MonoBehaviour {

    #region STATIC

    /// <summary>Global access point to the single <see cref="ShopManager"/> instance.</summary>
    public static ShopManager Instance { get; private set; }

    #endregion

    #region SERIALIZED FIELDS

    [SerializeField] private Character playerCharacter;

    #endregion

    #region FIELDS

    private bool isShopOpen = false;

    #endregion

    #region UNITY

    private void Awake() {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        ResolvePlayerCharacter();
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Ensures a valid reference to the player's character component.
    /// </summary>
    private void ResolvePlayerCharacter() {
        if (playerCharacter != null)
            return;

        playerCharacter = FindFirstObjectByType<Character>();
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

    #endregion
}
