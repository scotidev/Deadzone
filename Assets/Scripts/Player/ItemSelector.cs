using InfimaGames.LowPolyShooterPack;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// DEPRECATED: Item selection is now handled directly by Inventory.
/// This script is kept for backwards compatibility with existing input bindings.
/// </summary>
[System.Obsolete("ItemSelector is deprecated. Use Inventory.SelectByKeyNumber() instead.")]
public class ItemSelector : MonoBehaviour {

    [Header("References")]
    [Tooltip("Reference to the Inventory - selection logic is now unified here.")]
    [SerializeField] private Inventory inventory;

    [Header("Debug")]
    [Tooltip("Enable to see debug messages when keys are pressed.")]
    [SerializeField] private bool enableDebugLogs = true;

    private void Awake() {
        ValidateReferences();
    }

    private void ValidateReferences() {
        if (inventory == null) {
            Character character = GetComponent<Character>();
            if (character != null) {
                inventory = character.GetInventory() as Inventory;
            }
        }

        if (inventory == null) {
            inventory = FindFirstObjectByType<Inventory>();
        }

        if (inventory == null && enableDebugLogs) {
            Debug.LogWarning("[ItemSelector] Inventory reference is missing!");
        }
    }

    /// <summary>
    /// Called by Unity's Input System when numeric keys (1-9) are pressed.
    /// Now simply forwards to Inventory.SelectByKeyNumber() for unified handling.
    /// </summary>
    public void OnSelectWeapon(InputAction.CallbackContext context) {
        if (context.phase != InputActionPhase.Performed)
            return;

        if (inventory == null) {
            ValidateReferences();
            if (inventory == null) {
                Debug.LogWarning("[ItemSelector] Cannot process input - Inventory is null!");
                return;
            }
        }

        string path = context.control.path;
        char digitChar = path[path.Length - 1];
        int keyNumber = digitChar - '0';

        inventory.SelectByKeyNumber(keyNumber);
    }

    private void OnValidate() {
        ValidateReferences();
    }
}
