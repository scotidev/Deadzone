// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    public class Inventory : InventoryBehaviour
    {
        #region FIELDS
        
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

        #endregion
        
        #region METHODS
        
        public override void Init(int equippedAtStart = 0)
        {
            //Cache all weapons. Beware that weapons need to be parented to the object this component is on!
            weapons = GetComponentsInChildren<WeaponBehaviour>(true);
            
            Debug.Log($"[Inventory.Init] Called with equippedAtStart = {equippedAtStart}");
            Debug.Log($"[Inventory.Init] Found {weapons.Length} weapons:");
            for (int i = 0; i < weapons.Length; i++) {
                Debug.Log($"  [{i}] {weapons[i].name}");
            }
            
            //Disable all weapons. This makes it easier for us to only activate the one we need.
            foreach (WeaponBehaviour weapon in weapons)
                weapon.gameObject.SetActive(false);

            //Equip.
            Debug.Log($"[Inventory.Init] Now calling Equip({equippedAtStart})");
            Equip(equippedAtStart);
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
        private string GetWeaponIDForIndex(int index) {
            switch (index) {
                case 0: return "Pistol";
                case 1: return "SMG";
                case 2: return "Shotgun";
                case 3: return "Medkit";
                case 4: return "Grenades";
                // Indices 5-7 are buildables (handled by BuildingController)
                case 8: return "SpecialWeapon";
                default: return null;
            }
        }

        #endregion
    }
}