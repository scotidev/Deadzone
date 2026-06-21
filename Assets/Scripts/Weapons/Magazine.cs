using UnityEngine;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Magazine. Handles ammunition capacity by reading from WeaponDataSO and PlayerProgress
    /// to dynamically calculate magazine size based on weapon type and upgrade level.
    /// </summary>
    public class Magazine : MagazineBehaviour {
        
        #region SERIALIZED FIELDS

        [Header("Settings")]
        [SerializeField] private Sprite sprite;

        #endregion

        #region FIELDS

        private Weapon parentWeapon;

        #endregion

        #region UNITY

        private void Awake() {
            parentWeapon = GetComponentInParent<Weapon>();
        }

        #endregion

        #region METHODS

        #region GETTERS

        /// <summary>
        /// Returns the total ammunition capacity of this magazine.
        /// Calculated dynamically from WeaponDataSO and current upgrade level.
        /// Falls back to 1 if the system is not initialized.
        /// </summary>
        public override int GetAmmunitionTotal() {
            if (parentWeapon != null) {
                if (PlayerProgress.Instance != null) {
                    string itemID = parentWeapon.GetItemID();
                    int level = PlayerProgress.Instance.GetItemLevel(itemID);
                    
                    return PlayerProgress.Instance.GetItemMaxCurrent(itemID, level);
                }
            }
            
            return 1;
        }

        public override Sprite GetSprite() => sprite;

        #endregion

        #endregion
    }
}
