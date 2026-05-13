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
        // CONCEITO: Early return pattern prevents null reference errors.
        // If no item is equipped, skip this frame update.
        if (equippedItem == null)
            return;

        // NEW: Get item ID and fetch ammo from PlayerProgress (single source of truth)
        string itemID = equippedItem.GetItemID();
        int current = PlayerProgress.Instance.GetItemCurrent(itemID);
        int maxCurrent = PlayerProgress.Instance.GetItemMaxCurrent(itemID);
        int total = PlayerProgress.Instance.GetItemTotal(itemID);

        // Display current ammo in magazine/hand
        textMesh.text = current.ToString(CultureInfo.InvariantCulture);

        if (updateColor) {
            // Color based on how full the magazine/hand is
            float fillRatio = maxCurrent > 0 ? (current / (float)maxCurrent) : 0f;
            float colorAlpha = fillRatio * emptySpeed;
            textMesh.color = Color.Lerp(emptyColor, Color.white, colorAlpha);
        }
    }

        #endregion
    }
}