using UnityEngine;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Weapon Attachment Manager. Handles equipping and storing a Weapon's Attachments
    /// (scope, muzzle, magazine) and provides access to the currently equipped ones.
    /// </summary>
    public class WeaponAttachmentManager : WeaponAttachmentManagerBehaviour {

        #region SERIALIZED FIELDS

        [Header("Scope")]

        [Tooltip("Determines if the ironsights should be shown on the weapon model.")]
        [SerializeField] private bool scopeDefaultShow = true;

        [SerializeField] private ScopeBehaviour scopeDefaultBehaviour;

        [Header("Muzzle")]

        [SerializeField] private int muzzleIndex;

        [Tooltip("All possible Muzzle Attachments that this Weapon can use!")]
        [SerializeField] private MuzzleBehaviour[] muzzleArray;

        [Header("Magazine")]

        [SerializeField] private int magazineIndex;

        [Tooltip("All possible Magazine Attachments that this Weapon can use!")]
        [SerializeField]
        private Magazine[] magazineArray;

        #endregion

        #region FIELDS

        private ScopeBehaviour scopeBehaviour;
        private MuzzleBehaviour muzzleBehaviour;
        private MagazineBehaviour magazineBehaviour;

        #endregion

        #region UNITY

        protected override void Awake() {
            if (scopeBehaviour == null) {
                scopeBehaviour = scopeDefaultBehaviour;
                scopeBehaviour.gameObject.SetActive(scopeDefaultShow);
            }

            muzzleBehaviour = muzzleArray.SelectAndSetActive(muzzleIndex);

            magazineBehaviour = magazineArray.SelectAndSetActive(magazineIndex);
        }

        #endregion

        #region METHODS

        #region GETTERS

        public override ScopeBehaviour GetEquippedScope() => scopeBehaviour;
        public override ScopeBehaviour GetEquippedScopeDefault() => scopeDefaultBehaviour;

        public override MagazineBehaviour GetEquippedMagazine() => magazineBehaviour;
        public override MuzzleBehaviour GetEquippedMuzzle() => muzzleBehaviour;

        #endregion

        #endregion
    }
}
