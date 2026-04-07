using UnityEngine;
using UnityEngine.InputSystem;
using InfimaGames.LowPolyShooterPack;

/// <summary>
/// Handles weapon and buildable item selection via numeric keys (1-9).
/// Separates input handling from Character.cs to follow Single Responsibility Principle.
/// Keys 1-5 and 9 select weapons, keys 6-8 select buildable items.
/// </summary>
public class WeaponSelector : MonoBehaviour {
    
    [Header("References")]
    [Tooltip("Reference to the Character component to check cursor state and change weapons.")]
    [SerializeField] private Character character;
    
    [Tooltip("Reference to the Inventory to equip weapons.")]
    [SerializeField] private InventoryBehaviour inventory;

    [Header("Debug")]
    [Tooltip("Enable to see debug messages when keys are pressed.")]
    [SerializeField] private bool enableDebugLogs = true;

    /// <summary>
    /// Item names for each numeric key, used for debug messages and logging.
    /// </summary>
    private readonly string[] itemNames = {
        "Pistol",                  // Key 1
        "SMG",                     // Key 2
        "Shotgun",                 // Key 3
        "Med Kit",                 // Key 4
        "Grenade",                 // Key 5
        "Barricade",               // Key 6 - Buildable
        "Explosive Barrel",        // Key 7 - Buildable
        "Trap",                    // Key 8 - Buildable
        "Special"                  // Key 9
    };

    private void Awake() {
        // If references are not set in Inspector, try to find them automatically
        ValidateReferences();
    }

    /// <summary>
    /// Validates that all required references are set. If not, attempts to find them.
    /// This is a safety mechanism to prevent null reference errors.
    /// </summary>
    private void ValidateReferences() {
        // Try to get Character component if not assigned
        if (character == null) {
            character = GetComponent<Character>();
            if (character == null) {
                Debug.LogError("[WeaponSelector] Character reference is missing! Please assign it in the Inspector.");
            }
        }

        // Try to get Inventory component if not assigned
        if (inventory == null && character != null) {
            inventory = character.GetInventory();
            if (inventory == null) {
                Debug.LogError("[WeaponSelector] Inventory reference is missing and could not be found via Character.");
            }
        }
    }

    /// <summary>
    /// Generic method to handle weapon/item selection by key number.
    /// Can be called by individual Input Actions (OnSelectWeapon1, OnSelectWeapon2, etc.)
    /// or by a single unified action.
    /// </summary>
    private void SelectByKeyNumber(int keyNumber) {
        // Block input if cursor is unlocked (player is in a menu)
        if (character == null || !character.IsCursorLocked())
            return;

        // Validate key number is within expected range (1-9)
        if (keyNumber < 1 || keyNumber > 9) {
            Debug.LogWarning($"[WeaponSelector] Invalid key number: {keyNumber}. Expected 1-9.");
            return;
        }

        // Log which key was pressed (if debug enabled)
        if (enableDebugLogs) {
            Debug.Log($"[WeaponSelector] Key {keyNumber} pressed. Selected: {itemNames[keyNumber - 1]}");
        }

        // Check if this is a buildable item (keys 6, 7, 8)
        if (IsBuildableKey(keyNumber)) {
            SelectBuildable(keyNumber);
        }
        else {
            // This is a weapon (keys 1-5 or 9)
            SelectWeapon(keyNumber);
        }
    }

    /// <summary>
    /// Called by Unity's Input System when ANY numeric key binding (1-9) is pressed.
    /// Uses the InputAction.CallbackContext to detect WHICH specific key was pressed.
    /// This is the CORRECT way to handle multiple bindings on a single action.
    /// </summary>
    public void OnSelectWeapon(InputAction.CallbackContext context) {
        // Only process when the key is actually pressed (not released or held)
        if (context.phase != InputActionPhase.Performed)
            return;

        // Block input if cursor is unlocked (player is in a menu)
        if (character == null || !character.IsCursorLocked())
            return;

        // Get the control that triggered this action (the actual key that was pressed)
        // context.control contains information about which specific binding was activated
        var control = context.control;

        // Extract the key number from the control's path
        // Path format: "<Keyboard>/1", "<Keyboard>/2", etc.
        // We take the last character which is the digit
        string path = control.path;
        
        // Get the digit character from the path (last character)
        char digitChar = path[path.Length - 1];
        
        // Convert the character to an integer (1-9)
        // This works because char '1' can be converted to int 1, etc.
        int keyNumber = digitChar - '0';

        // Log the detected key for debugging
        if (enableDebugLogs) {
            Debug.Log($"[WeaponSelector] Detected key press from control: {path} → Key {keyNumber}");
        }

        // Call the unified selection logic
        SelectByKeyNumber(keyNumber);
    }

    /// <summary>
    /// Checks if the given key number corresponds to a buildable item.
    /// Buildable items are on keys 6, 7, and 8.
    /// </summary>
    private bool IsBuildableKey(int keyNumber) {
        return keyNumber >= 6 && keyNumber <= 8;
    }

    /// <summary>
    /// Selects a buildable item by delegating to the BuildingController.
    /// Buildable items (keys 6-8) are handled by a separate system for placing objects in the world.
    /// </summary>
    private void SelectBuildable(int keyNumber) {
        // Check if BuildingController exists in the scene
        if (BuildingController.Instance == null) {
            Debug.LogWarning("[WeaponSelector] BuildingController not found! Cannot select buildable item.");
            return;
        }

        // Convert key number to buildable slot (6->1, 7->2, 8->3)
        int buildableSlot = keyNumber - 5;
        
        // Delegate to BuildingController to handle the buildable selection
        BuildingController.Instance.SelectBuildableBySlot(buildableSlot);
    }

    /// <summary>
    /// Selects a weapon by equipping it from the inventory.
    /// Handles weapon index mapping and delegates to the Character's Equip coroutine.
    /// Validates if weapon is unlocked before attempting to equip.
    /// </summary>
    private void SelectWeapon(int keyNumber) {
        // Validate inventory exists
        if (inventory == null) {
            Debug.LogWarning("[WeaponSelector] Inventory reference is missing! Cannot select weapon.");
            ValidateReferences(); // Try to find references again
            return;
        }

        // Convert key number to weapon index in the inventory array
        int weaponIndex = GetWeaponIndex(keyNumber);

        // Get the current weapon's index to check if we're trying to equip the same weapon
        int currentIndex = inventory.GetEquippedIndex();

        // Check if we're trying to equip the same weapon
        if (currentIndex == weaponIndex) {
            if (enableDebugLogs) {
                Debug.Log($"[WeaponSelector] {itemNames[keyNumber - 1]} is already equipped.");
            }
            return;
        }

        // Check if weapon is unlocked (except Pistol)
        if (PlayerProgress.Instance != null && weaponIndex != 0) {
            string weaponID = GetWeaponIDForIndex(weaponIndex);
            if (!string.IsNullOrEmpty(weaponID) && !PlayerProgress.Instance.IsWeaponUnlocked(weaponID)) {
                if (enableDebugLogs) {
                    Debug.Log($"[WeaponSelector] {itemNames[keyNumber - 1]} is locked! Visit the shop to unlock it.");
                }
                // TODO: Play error sound (Phase 7)
                return;
            }
        }

        // Try to equip the weapon
        EquipWeaponAtIndex(weaponIndex, keyNumber);
    }

    /// <summary>
    /// Maps weapon index to weapon ID for unlock checking.
    /// Must match Inventory.GetWeaponIDForIndex().
    /// </summary>
    private string GetWeaponIDForIndex(int weaponIndex) {
        switch (weaponIndex) {
            case 0: return "Pistol";
            case 1: return "SMG";
            case 2: return "Shotgun";
            case 3: return "Medkit";
            case 4: return "Grenades";
            case 8: return "SpecialWeapon";
            default: return null;
        }
    }

    /// <summary>
    /// Converts a key number (1-9) to the corresponding weapon index in the inventory.
    /// Keys 1-5 map directly to indices 0-4.
    /// Key 9 maps to index 8 (skipping buildables at indices 5-7).
    /// </summary>
    private int GetWeaponIndex(int keyNumber) {
        if (keyNumber <= 5) {
            // Keys 1-5 map to indices 0-4
            return keyNumber - 1;
        }
        else {
            // Key 9 maps to index 8 (Special weapon)
            // In the future, this could be expanded if more weapons are added
            return 8;
        }
    }

    /// <summary>
    /// Attempts to equip a weapon at the specified index.
    /// This will trigger the Character's equip animation if the weapon exists.
    /// </summary>
    private void EquipWeaponAtIndex(int weaponIndex, int keyNumber) {
        // Use Character's TryEquipWeapon to properly handle animations and setup
        bool success = character.TryEquipWeapon(weaponIndex);

        // Log the result
        if (success) {
            if (enableDebugLogs) {
                Debug.Log($"[WeaponSelector] Successfully equipped {itemNames[keyNumber - 1]} at index {weaponIndex}.");
            }
        }
        else {
            if (enableDebugLogs) {
                Debug.Log($"[WeaponSelector] Weapon at index {weaponIndex} ({itemNames[keyNumber - 1]}) does not exist in inventory yet.");
            }
        }
    }

    #region EDITOR HELPERS
    
    /// <summary>
    /// Called when the script is loaded or a value is changed in the Inspector.
    /// Automatically tries to find references if they're missing.
    /// </summary>
    private void OnValidate() {
        if (character == null) {
            character = GetComponent<Character>();
        }
        
        if (inventory == null && character != null) {
            inventory = character.GetInventory();
        }
    }
    
    #endregion
}
