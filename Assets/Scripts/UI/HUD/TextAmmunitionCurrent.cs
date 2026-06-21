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

        /// <summary>
        /// Updates the current ammunition display text and color based on magazine fill ratio.
        /// </summary>
        protected override void Tick() {
            if (equippedItem == null)
                return;

            string itemID = equippedItem.GetItemID();
            int current = PlayerProgress.Instance.GetItemCurrent(itemID);
            int maxCurrent = PlayerProgress.Instance.GetItemMaxCurrent(itemID);
            int total = PlayerProgress.Instance.GetItemTotal(itemID);

            textMesh.text = current.ToString(CultureInfo.InvariantCulture);

            if (updateColor) {
                float fillRatio = maxCurrent > 0 ? (current / (float)maxCurrent) : 0f;
                float colorAlpha = fillRatio * emptySpeed;
                textMesh.color = Color.Lerp(emptyColor, Color.white, colorAlpha);
            }
        }

        #endregion
    }
}
