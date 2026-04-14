using InfimaGames.LowPolyShooterPack;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles item selection via numeric keys (1-9).
/// Centralizes the logic to select both weapons and buildable items using the ISelectableItem interface.
/// </summary>
public class ItemSelector : MonoBehaviour {

    [Header("References")]
    [Tooltip("Reference to the Character component to check cursor state and get inventory.")]
    [SerializeField] private Character character;

    [Tooltip("Reference to the Inventory to equip weapons.")]
    [SerializeField] private Inventory inventory;

    [Tooltip("Reference to the BuildingController to equip buildables.")]
    [SerializeField] private BuildingController buildingController;

    [Header("Debug")]
    [Tooltip("Enable to see debug messages when keys are pressed.")]
    [SerializeField] private bool enableDebugLogs = true;

    /// <summary>
    /// Item names for each numeric key, used for debug messages and logging.
    /// </summary>
    private readonly string[] itemNames = {
        "Pistol",                  // Key 1 (Index 0)
        "AK47",                    // Key 2 (Index 1)
        "Shotgun",                 // Key 3 (Index 2)
        "Med Kit",                 // Key 4 (Index 3)
        "Grenade",                 // Key 5 (Index 4)
        "Barricade",               // Key 6 (Slot 1) - Buildable
        "Explosive Barrel",        // Key 7 (Slot 2) - Buildable
        "Trap",                    // Key 8 (Slot 3) - Buildable
        "Special"                  // Key 9 (Index 8)
    };

    private void Awake() {
        ValidateReferences();
    }

    /// <summary>
    /// Validates that all required references are set. If not, attempts to find them.
    /// </summary>
    private void ValidateReferences() {
        if (character == null) {
            character = GetComponent<Character>();
            if (character == null) {
                Debug.LogError("[ItemSelector] Character reference is missing!");
            }
        }

        if (inventory == null && character != null) {
            inventory = character.GetInventory() as Inventory;
            if (inventory == null) {
                Debug.LogError("[ItemSelector] Inventory reference is missing or is not of type Inventory.");
            }
        }

        if (buildingController == null) {
            buildingController = FindFirstObjectByType<BuildingController>();
            if (buildingController == null) {
                Debug.LogWarning("[ItemSelector] BuildingController reference is missing.");
            }
        }
    }

    /// <summary>
    /// Called by Unity's Input System when ANY numeric key binding (1-9) is pressed.
    /// </summary>
    public void OnSelectWeapon(InputAction.CallbackContext context) {
        if (context.phase != InputActionPhase.Performed)
            return;

        // Verify if we are actually playing the game (not in a menu)
        // We use IsInterfaceMode here to be more robust than just checking if the cursor is locked,
        // since cursor state might sometimes desync or take a frame to update when closing UI.
        if (character == null || character.IsInterfaceMode())
            return;

        string path = context.control.path;
        char digitChar = path[path.Length - 1];
        int keyNumber = digitChar - '0';

        SelectByKeyNumber(keyNumber);
    }

    /// <summary>
    /// Handles the generic logic for selecting an item based on the key pressed.
    /// </summary>
    private void SelectByKeyNumber(int keyNumber) {
        if (keyNumber < 1 || keyNumber > 9) {
            Debug.LogWarning($"[ItemSelector] Invalid key number: {keyNumber}. Expected 1-9.");
            return;
        }

        if (enableDebugLogs) {
            Debug.Log($"[ItemSelector] Key {keyNumber} pressed. Attempting to select: {itemNames[keyNumber - 1]}");
        }

        if (IsBuildableKey(keyNumber)) {
            HandleBuildableSelection(keyNumber);
        }
        else {
            HandleWeaponSelection(keyNumber);
        }
    }

    private bool IsBuildableKey(int keyNumber) {
        return keyNumber >= 6 && keyNumber <= 8;
    }

    /// <summary>
    /// Delegates buildable selection to the BuildingController.
    /// </summary>
    private void HandleBuildableSelection(int keyNumber) {
        if (buildingController == null) {
            Debug.LogWarning("[ItemSelector] BuildingController not found! Cannot select buildable item.");
            return;
        }

        int buildableSlot = keyNumber - 5; // Key 6 -> Slot 1, etc.
        buildingController.SelectBuildableBySlot(buildableSlot);

        // Treat the BuildingController itself as the selectable item
        ISelectableItem selectable = buildingController as ISelectableItem;
        selectable?.Select();
    }

    /// <summary>
    /// Delegates weapon selection to the Inventory, checking unlocks first.
    /// </summary>
    private void HandleWeaponSelection(int keyNumber) {
        if (inventory == null) {
            Debug.LogWarning("[ItemSelector] Inventory reference is missing! Cannot select weapon.");
            ValidateReferences();
            return;
        }

        int weaponIndex = GetWeaponIndex(keyNumber);

        if (inventory.GetEquippedIndex() == weaponIndex) {
            if (enableDebugLogs) {
                Debug.Log($"[ItemSelector] {itemNames[keyNumber - 1]} is already equipped.");
            }
            return;
        }

        if (PlayerProgress.Instance != null && weaponIndex != 0) {
            string weaponID = inventory.GetWeaponIDForIndex(weaponIndex);
            if (!string.IsNullOrEmpty(weaponID) && !PlayerProgress.Instance.IsWeaponUnlocked(weaponID)) {
                if (enableDebugLogs) {
                    Debug.Log($"[ItemSelector] {itemNames[keyNumber - 1]} is locdaked!");
                }
                return;
            }
        }

        // Inform character to start equip coroutine, which eventually calls Inventory.Equip
        bool success = character.TryEquipWeapon(weaponIndex);

        if (success && enableDebugLogs) {
            Debug.Log($"[ItemSelector] Successfully started equip for {itemNames[keyNumber - 1]} at index {weaponIndex}.");
        }
    }

    /// <summary>
    /// Converts a key number (1-9) to the corresponding weapon index in the inventory.
    /// </summary>
    private int GetWeaponIndex(int keyNumber) {
        if (keyNumber <= 5) {
            return keyNumber - 1;
        }
        else {
            return 8; // Key 9 maps to special weapon
        }
    }

    private void OnValidate() {
        if (character == null) {
            character = GetComponent<Character>();
        }

        if (inventory == null && character != null) {
            inventory = character.GetInventory() as Inventory;
        }
    }
}
