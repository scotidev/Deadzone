using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Manages the player's item selection and equipping logic.
    /// Handles weapons, buildables, and consumables through a unified slot system.
    /// </summary>
    public class Inventory : InventoryBehaviour {

        #region SERIALIZED FIELDS

        [Tooltip("Reference to the BuildingController for placement logic.")]
        [SerializeField] private BuildingController buildingController;

        #endregion

        #region FIELDS

        private ItemBehaviour[] selectableItems;
        private ItemBehaviour currentlySelected;
        private int currentSelectionIndex = -1;

        private WeaponBehaviour[] weapons;
        private WeaponBehaviour equipped;
        private int equippedIndex = -1;

        private Character character;

        private readonly Dictionary<int, int> keyToIndex = new() {
            { 1, 0 }, { 2, 1 }, { 3, 2 }, { 4, 3 },
            { 5, 4 }, { 6, 5 }, { 7, 6 }, { 8, 7 }
        };

        #endregion

        #region UNITY

        private void Awake() {
            ResolvePlayerCharacter();
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Initializes the inventory by discovering all child ItemBehaviour components,
        /// filtering out passive items (Vest), and selecting the starting item.
        /// </summary>
        public override void Init(int equippedAtStart = 0) {
            ItemBehaviour[] allItems = GetComponentsInChildren<ItemBehaviour>(true);

            var filteredItems = new List<ItemBehaviour>();
            for (int i = 0; i < allItems.Length; i++) {
                if (allItems[i] is Vest) {
                    continue;
                }
                filteredItems.Add(allItems[i]);
            }
            selectableItems = filteredItems.ToArray();

            WeaponBehaviour[] allWeapons = GetComponentsInChildren<WeaponBehaviour>(true);
            var filteredWeapons = new List<WeaponBehaviour>();
            for (int i = 0; i < allWeapons.Length; i++) {
                filteredWeapons.Add(allWeapons[i]);
            }
            weapons = filteredWeapons.ToArray();

            foreach (ItemBehaviour item in selectableItems)
                item.gameObject.SetActive(false);

            SelectItem(equippedAtStart);
        }

        /// <summary>
        /// Maps the input context from numeric keys (1-8) to the corresponding item index.
        /// Returns -1 if the input is invalid.
        /// </summary>
        public int GetIndexFromInput(InputAction.CallbackContext context) {
            string path = context.control.path;
            char digitChar = path[path.Length - 1];
            int keyNumber = digitChar - '0';

            if (keyToIndex.TryGetValue(keyNumber, out int itemIndex)) {
                return itemIndex;
            }
            return -1;
        }

        /// <summary>
        /// Select item by index. Deselects previous item and selects new one.
        /// This method is public so Character can call it mid-coroutine for smooth transitions.
        /// </summary>
        public void SelectItem(int index) {
            if (selectableItems == null || index < 0 || index >= selectableItems.Length) {
                return;
            }

            ItemBehaviour newItem = selectableItems[index];
            if (newItem == null) {
                return;
            }

            if (!newItem.CanBeUsed()) {
                return;
            }

            if (currentlySelected != null) {
                currentlySelected.OnDeselected();
                currentlySelected.gameObject.SetActive(false);
            }

            currentlySelected = newItem;
            currentSelectionIndex = index;

            currentlySelected.gameObject.SetActive(true);
            currentlySelected.OnSelected();

            if (newItem is WeaponBehaviour weapon) {
                UpdateEquippedWeapon(weapon);
            } else {
                equipped = null;
                equippedIndex = -1;
                if (character != null) character.RefreshWeaponSetup();
            }
        }

        /// <summary>
        /// Updates equipped weapon reference for Character compatibility.
        /// Maintains compatibility with Character.TryEquipWeapon() and the existing weapon system.
        /// </summary>
        private void UpdateEquippedWeapon(WeaponBehaviour weapon) {
            if (weapons == null) {
                return;
            }

            for (int i = 0; i < weapons.Length; i++) {
                if (weapons[i] == weapon) {
                    equippedIndex = i;
                    equipped = weapon;

                    if (character != null) {
                        character.RefreshWeaponSetup();
                    }
                    return;
                }
            }
        }

        /// <summary>
        /// Restores the last equipped weapon after buildable placement is canceled.
        /// Called by BuildingController when player finishes placing a buildable.
        /// </summary>
        public void RestoreLastWeapon() {
            for (int i = 0; i < selectableItems.Length; i++) {
                ItemBehaviour item = selectableItems[i];
                if (item is WeaponBehaviour weapon && weapon.CanBeUsed()) {
                    SelectItem(i);
                    return;
                }
            }
        }

        /// <summary>
        /// Resolves the Character reference needed for weapon selection.
        /// Uses GetComponentInParent to find Character in the parent hierarchy.
        /// </summary>
        private void ResolvePlayerCharacter() {
            if (character == null) {
                character = GetComponentInParent<Character>();
            }
        }

        /// <summary>
        /// Equips a weapon by index. Legacy method kept for compatibility.
        /// Validates unlock status and updates the equipped weapon reference.
        /// </summary>
        public override WeaponBehaviour Equip(int index) {
            if (weapons == null || index > weapons.Length - 1 || equippedIndex == index)
                return equipped;

            if (index != 0 && PlayerProgress.Instance != null) {
                string weaponID = GetWeaponIDForIndex(index);
                if (!string.IsNullOrEmpty(weaponID) && !PlayerProgress.Instance.IsWeaponUnlocked(weaponID))
                    return equipped;
            }

            if (equipped != null)
                equipped.gameObject.SetActive(false);

            equippedIndex = index;
            equipped = weapons[equippedIndex];
            equipped.gameObject.SetActive(true);

            return equipped;
        }

        #endregion

        #region GETTERS

        public override int GetLastIndex() {
            int newIndex = equippedIndex - 1;
            if (newIndex < 0)
                newIndex = weapons.Length - 1;
            return newIndex;
        }

        public override int GetNextIndex() {
            int newIndex = equippedIndex + 1;
            if (newIndex > weapons.Length - 1)
                newIndex = 0;
            return newIndex;
        }

        public override WeaponBehaviour GetEquipped() => equipped;
        public override int GetEquippedIndex() => equippedIndex;

        /// <summary>
        /// Returns the current selection index (0-7), representing any item in hand.
        /// Unlike equippedIndex (which focuses on weapons), this index reflects exactly
        /// what the player is holding at the moment.
        /// </summary>
        public int GetSelectionIndex() => currentSelectionIndex;

        public override ItemBehaviour GetEquippedItem() => currentlySelected;

        /// <summary>Returns the 0-based slot index in the selectable items array for the given itemID. Returns -1 if not found.</summary>
        public int GetSlotIndexForItemID(string itemID) {
            if (selectableItems == null || string.IsNullOrEmpty(itemID)) return -1;
            for (int i = 0; i < selectableItems.Length; i++) {
                if (selectableItems[i] != null && selectableItems[i].GetItemID() == itemID)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Maps weapon array index to weapon ID for unlock checking.
        /// This mapping should match the shop item order.
        /// </summary>
        public string GetWeaponIDForIndex(int index) {
            switch (index) {
                case 0: return "1";
                case 1: return "2";
                case 2: return "3";
                case 3: return "4";
                case 4: return "5";
                default: return null;
            }
        }

        /// <summary>
        /// Selects the weapon at the currently equipped index.
        /// </summary>
        public void ReEquipCurrentItem() {
            Equip(equippedIndex);
        }

        /// <summary>
        /// Attempts to use the currently equipped item.
        /// If it's a weapon, does nothing (Character handles weapon firing).
        /// If it's a consumable/buildable, calls OnUse().
        /// </summary>
        public override void TryUseEquippedItem() {
            if (currentlySelected is WeaponBehaviour) {
                return;
            }

            currentlySelected?.OnUse();
        }

        #endregion
    }
}
