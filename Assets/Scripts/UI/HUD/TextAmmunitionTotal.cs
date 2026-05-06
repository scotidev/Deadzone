// Copyright 2021, Infima Games. All Rights Reserved.

using System.Globalization;

namespace InfimaGames.LowPolyShooterPack.Interface {
    /// <summary>
    /// Total Ammunition Text.
    /// </summary>
    public class TextAmmunitionTotal : ElementText {
        #region METHODS

        /// <summary>
        /// Updates the total ammunition text.
        /// First principles: Safety check added to ensure weapon is valid before access.
        /// </summary>
        protected override void Tick() {
            // Check if the weapon exists before accessing it
            if (equippedWeapon == null) return;

            float ammunitionTotal = equippedWeapon.GetAmmunitionTotal();

            textMesh.text = ammunitionTotal.ToString(CultureInfo.InvariantCulture);
        }

        #endregion
    }
}