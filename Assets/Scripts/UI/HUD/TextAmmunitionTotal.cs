// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;
using System.Globalization;

namespace InfimaGames.LowPolyShooterPack.Interface {
    /// <summary>
    /// Total Ammunition Text.
    /// </summary>
    public class TextAmmunitionTotal : ElementText {
        #region METHODS

        /// <summary>
        /// Updates the total ammunition text.
        /// Displays the inventory quantity (not in current use/magazine).
        /// First principles: Queries PlayerProgress for single source of truth.
        /// </summary>
        protected override void Tick() {
            // Check if the item is equipped before accessing it
            if (equippedItem == null) return;

            // Get item ID and fetch total ammo from PlayerProgress (inventory/reserve)
            string itemID = equippedItem.GetItemID();
            int total = PlayerProgress.Instance.GetItemTotal(itemID);

            // FIXED: For non-weapon items (buildables, consumables), subtract what's in hand (current)
            // from the total so the player sees the actual reserve quantity.
            // For example: if the player owns 10 bear traps and has 1 in hand selected,
            // show total as 9 (reserve) instead of 10 (inventory including what's in hand).
            // Weapons already track reserve separately from magazine, so this is only needed
            // for buildables and consumables where total includes the "in hand" item.
            if (!(equippedItem is WeaponBehaviour)) {
                int current = PlayerProgress.Instance.GetItemCurrent(itemID);
                total = Mathf.Max(0, total - current);
            }

            textMesh.text = total.ToString(CultureInfo.InvariantCulture);
        }

        #endregion
    }
}