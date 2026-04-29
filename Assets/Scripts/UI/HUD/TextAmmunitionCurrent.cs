// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;
using System.Globalization;

namespace InfimaGames.LowPolyShooterPack.Interface {
    /// <summary>
    /// Current Ammunition Text.
    /// </summary>
    public class TextAmmunitionCurrent : ElementText {

        #region SERIALIZED FIELDS

        [Header("Colors")]

        [SerializeField] private bool updateColor = true;

        [Tooltip("Determines how fast the color changes as the ammunition is fired.")]
        [SerializeField] private float emptySpeed = 1.5f;

        [SerializeField] private Color emptyColor = Color.red;

        #endregion

        #region METHODS

        protected override void Tick() {
            float current = equippedWeapon.GetAmmunitionCurrent();
            float total = equippedWeapon.GetAmmunitionTotal();

            textMesh.text = current.ToString(CultureInfo.InvariantCulture);

            if (updateColor) {
                float colorAlpha = (current / total) * emptySpeed;
                textMesh.color = Color.Lerp(emptyColor, Color.white, colorAlpha);
            }
        }

        #endregion
    }
}