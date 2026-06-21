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
        /// Queries PlayerProgress for single source of truth.
        /// </summary>
        protected override void Tick() {
            if (equippedItem == null) return;

            string itemID = equippedItem.GetItemID();
            int total = PlayerProgress.Instance.GetItemTotal(itemID);

            if (!(equippedItem is WeaponBehaviour)) {
                int current = PlayerProgress.Instance.GetItemCurrent(itemID);
                total = Mathf.Max(0, total - current);
            }

            textMesh.text = total.ToString(CultureInfo.InvariantCulture);
        }

        #endregion
    }
}
