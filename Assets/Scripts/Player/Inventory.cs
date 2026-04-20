// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;
using UnityEngine.InputSystem;

namespace InfimaGames.LowPolyShooterPack
{
    public class Inventory : InventoryBehaviour, ISelectableItem
    {
        #region FIELDS

        [Header("Building Controller")]
        [Tooltip("Reference to the BuildingController for placement logic.")]
        [SerializeField] private BuildingController buildingController;

        [Header("Debug")]
        [Tooltip("Enable debug logs for item selection.")]
        [SerializeField] private bool enableDebugLogs = true;

        /// <summary>
        /// Array of all weapons. These are gotten in the order that they are parented to this object.
        /// </summary>
        private WeaponBehaviour[] weapons;

        /// <summary>
        /// Currently equipped WeaponBehaviour.
        /// </summary>
        private WeaponBehaviour equipped;
        /// <summary>
        /// Currently equipped index.
        /// </summary>
        private int equippedIndex = -1;

        /// <summary>
        /// Reference to the Character for checking interface mode.
        /// </summary>
        private Character character;

        /// <summary>
        /// Item names for each numeric key (1-9).
        /// </summary>
        private readonly string[] itemNames = {
            "Pistol",         // Key 1 (Index 0)
            "AK47",           // Key 2 (Index 1)
            "Shotgun",        // Key 3 (Index 2)
            "Med Kit",        // Key 4 (Index 3)
            "Grenade",       // Key 4 (Index 4)
            "Barricade",     // Key 6 (Buildable 1)
            "Explosive Barrel", // Key 7 (Buildable 2)
            "Bear Trap",     // Key 8 (Buildable 3)
            "Special"        // Key 9 (Index 8)
        };

        #endregion
        
        #region METHODS

        private void Awake() {
            character = GetComponent<Character>();
        }

        public override void Init(int equippedAtStart = 0)
        {
            //Cache all weapons. Beware that weapons need to be parented to the object this component is on!
            weapons = GetComponentsInChildren<WeaponBehaviour>(true);

            if (enableDebugLogs) {
                Debug.Log($"[Inventory.Init] Called with equippedAtStart = {equippedAtStart}");
                Debug.Log($"[Inventory.Init] Found {weapons.Length} weapons:");
                for (int i = 0; i < weapons.Length; i++) {
                    Debug.Log($"  [{i}] {weapons[i].name}");
                }
            }

            //Disable all weapons. This makes it easier for us to only activate the one we need.
            foreach (WeaponBehaviour weapon in weapons)
                weapon.gameObject.SetActive(false);

            //Equip.
            if (enableDebugLogs) {
                Debug.Log($"[Inventory.Init] Now calling Equip({equippedAtStart})");
            }
            Equip(equippedAtStart);
        }

        /// <summary>
        /// Called by Input System when any numeric key (1-9) is pressed.
        /// Handles both weapons and buildables in a unified way.
        /// </summary>
        public void OnSelectItem(InputAction.CallbackContext context) {
            if (context.phase != InputActionPhase.Performed)
                return;

            // Don't process if in interface/paused mode
            if (character != null && character.IsInterfaceMode())
                return;

            string path = context.control.path;
            char digitChar = path[path.Length - 1];
            int keyNumber = digitChar - '0';

            SelectByKeyNumber(keyNumber);
        }

        /// <summary>
        /// Unified item selection by key number (1-9).
        /// Keys 1-5, 9 select weapons. Keys 6-8 select buildables.
        /// </summary>
        public void SelectByKeyNumber(int keyNumber) {
            if (keyNumber < 1 || keyNumber > 9) {
                Debug.LogWarning($"[Inventory.SelectByKeyNumber] Invalid key number: {keyNumber}. Expected 1-9.");
                return;
            }

            if (enableDebugLogs) {
                Debug.Log($"[Inventory.SelectByKeyNumber] Key {keyNumber} pressed: {itemNames[keyNumber - 1]}");
            }

            // Keys 6-8 are buildables
            if (keyNumber >= 6 && keyNumber <= 8) {
                SelectBuildable(keyNumber);
            }
            else {
                // Keys 1-5 and 9 are weapons
                SelectWeapon(keyNumber);
            }
        }

        /// <summary>
        /// Handles buildable item selection.
        /// </summary>
        private void SelectBuildable(int keyNumber) {
            if (buildingController == null) {
                Debug.LogWarning("[Inventory.SelectBuildable] BuildingController is null!");
                buildingController = FindFirstObjectByType<BuildingController>();
                if (buildingController == null) {
                    Debug.LogError("[Inventory.SelectBuildable] Cannot find BuildingController!");
                    return;
                }
            }

            int buildableSlot = keyNumber - 5; // Key 6 -> Slot 1, Key 7 -> Slot 2, Key 8 -> Slot 3
            buildingController.SelectBuildableBySlot(buildableSlot);
        }

        /// <summary>
        /// Handles weapon/item selection.
        /// </summary>
        private void SelectWeapon(int keyNumber) {
            // Cancel any active building placement
            if (BuildingController.Instance != null && BuildingController.Instance.IsPlacing) {
                BuildingController.Instance.CancelPlacement();
            }

            // Calculate weapon index from key number
            int weaponIndex;
            if (keyNumber <= 5) {
                weaponIndex = keyNumber - 1;
            }
            else {
                weaponIndex = 8; // Key 9 maps to special weapon (index 8)
            }

            // Check if already equipped
            if (equippedIndex == weaponIndex) {
                if (enableDebugLogs) {
                    Debug.Log($"[Inventory.SelectWeapon] {itemNames[keyNumber - 1]} is already equipped.");
                }
                return;
            }

            // Check if weapon is unlocked (except Pistol at index 0)
            if (weaponIndex != 0 && PlayerProgress.Instance != null) {
                string weaponID = GetWeaponIDForIndex(weaponIndex);
                if (!string.IsNullOrEmpty(weaponID) && !PlayerProgress.Instance.IsWeaponUnlocked(weaponID)) {
                    if (enableDebugLogs) {
                        Debug.Log($"[Inventory.SelectWeapon] {itemNames[keyNumber - 1]} is locked!");
                    }
                    return;
                }
            }

            // Equip the weapon via Character
            if (character != null) {
                bool success = character.TryEquipWeapon(weaponIndex);
                if (success && enableDebugLogs) {
                    Debug.Log($"[Inventory.SelectWeapon] Equipped {itemNames[keyNumber - 1]} at index {weaponIndex}.");
                }
            }
            else {
                // Fallback: equip directly
                Equip(weaponIndex);
            }
        }

        public override WeaponBehaviour Equip(int index)
        {
            Debug.Log($"[Inventory.Equip] Called with index = {index}, current equippedIndex = {equippedIndex}");
            
            //If we have no weapons, we can't really equip anything.
            if (weapons == null) {
                Debug.LogWarning("[Inventory.Equip] weapons array is null!");
                return equipped;
            }
            
            //The index needs to be within the array's bounds.
            if (index > weapons.Length - 1) {
                Debug.LogWarning($"[Inventory.Equip] Index {index} is out of bounds (max = {weapons.Length - 1})");
                return equipped;
            }

            //No point in allowing equipping the already-equipped weapon.
            if (equippedIndex == index) {
                Debug.Log($"[Inventory.Equip] Weapon at index {index} is already equipped. Skipping.");
                return equipped;
            }

            // Check if weapon is unlocked (except Pistol at index 0 which is always unlocked)
            if (index != 0 && PlayerProgress.Instance != null) {
                string weaponID = GetWeaponIDForIndex(index);
                if (!string.IsNullOrEmpty(weaponID) && !PlayerProgress.Instance.IsWeaponUnlocked(weaponID)) {
                    Debug.LogWarning($"[Inventory.Equip] Weapon at index {index} ({weaponID}) is locked!");
                    return equipped;
                }
            }
            
            //Disable the currently equipped weapon, if we have one.
            if (equipped != null) {
                Debug.Log($"[Inventory.Equip] Disabling current weapon: {equipped.name}");
                equipped.gameObject.SetActive(false);
            }

            //Update index.
            equippedIndex = index;
            //Update equipped.
            equipped = weapons[equippedIndex];
            //Activate the newly-equipped weapon.
            equipped.gameObject.SetActive(true);
            
            Debug.Log($"[Inventory.Equip] Successfully equipped: {equipped.name} at index {equippedIndex}");

            //Return.
            return equipped;
        }
        
        #endregion

        #region Getters

        public override int GetLastIndex()
        {
            //Get last index with wrap around.
            int newIndex = equippedIndex - 1;
            if (newIndex < 0)
                newIndex = weapons.Length - 1;

            //Return.
            return newIndex;
        }

        public override int GetNextIndex()
        {
            //Get next index with wrap around.
            int newIndex = equippedIndex + 1;
            if (newIndex > weapons.Length - 1)
                newIndex = 0;

            //Return.
            return newIndex;
        }

        public override WeaponBehaviour GetEquipped() => equipped;
        public override int GetEquippedIndex() => equippedIndex;

        /// <summary>
        /// Maps weapon array index to weapon ID for unlock checking.
        /// This mapping should match the shop item order.
        /// </summary>
        public string GetWeaponIDForIndex(int index) {
            switch (index) {
                case 0: return "1"; // Pistol
                case 1: return "2"; // AK47
                case 2: return "3"; // Shotgun
                case 3: return "4"; // Medkit
                case 4: return "5"; // Grenades
                // Buildables (indices 5-7) are handled by BuildingController
                case 8: return "9"; // SpecialWeapon
                default: return null;
            }
        }

        /// <summary>
        /// Selects the weapon at the currently equipped index.
        /// This method is part of the ISelectableItem interface, allowing weapons to be selected generically.
        /// </summary>
        public void Select()
        {
            Equip(equippedIndex);
        }

        #endregion
    }
}