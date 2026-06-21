using UnityEngine;
using UnityEngine.UI;

namespace InfimaGames.LowPolyShooterPack.Interface {
    /// <summary>
    /// Item Image. Displays the icon of the currently equipped item (weapon, medkit, grenade, buildable, etc).
    /// Uses the unified GetIcon() method from ItemBehaviour, so it works for ALL item types.
    /// </summary>
    public class ImageItem : Element {

        #region SERIALIZED FIELDS

        [Header("Settings")]

        [Tooltip("Image component to display the item icon.")]
        [SerializeField] private Image itemImage;

        #endregion

        #region METHODS

        /// <summary>
        /// Updates the item icon. Gets the icon from the currently equipped item.
        /// Gracefully handles null icons - just hides the image if no icon is available.
        /// </summary>
        protected override void Tick() {
            if (equippedItem == null) {
                itemImage.enabled = false;
                return;
            }

            Sprite icon = equippedItem.GetIcon();

            if (icon != null) {
                itemImage.sprite = icon;
                itemImage.enabled = true;
            } else {
                itemImage.enabled = false;
            }
        }

        #endregion
    }
}
