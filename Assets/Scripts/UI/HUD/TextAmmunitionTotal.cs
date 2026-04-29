// Copyright 2021, Infima Games. All Rights Reserved.

using System.Globalization;

namespace InfimaGames.LowPolyShooterPack.Interface {
    /// <summary>
    /// Total Ammunition Text.
    /// </summary>
    public class TextAmmunitionTotal : ElementText {
        #region METHODS

        protected override void Tick() {
            float ammunitionTotal = equippedWeapon.GetAmmunitionTotal();

            textMesh.text = ammunitionTotal.ToString(CultureInfo.InvariantCulture);
        }

        #endregion
    }
}