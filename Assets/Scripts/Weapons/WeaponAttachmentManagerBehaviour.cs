using UnityEngine;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Weapon Attachment Manager Behaviour. Abstract base class providing access to
    /// equipped weapon attachments (scope, magazine, muzzle).
    /// </summary>
    public abstract class WeaponAttachmentManagerBehaviour : MonoBehaviour {
        #region UNITY

        protected virtual void Awake() { }

        protected virtual void Start() { }

        protected virtual void Update() { }

        protected virtual void LateUpdate() { }

        #endregion

        #region METHODS

        #region GETTERS

        /// <summary>
        /// Returns the equipped scope.
        /// </summary>
        public abstract ScopeBehaviour GetEquippedScope();
        /// <summary>
        /// Returns the equipped scope default.
        /// </summary>
        public abstract ScopeBehaviour GetEquippedScopeDefault();

        /// <summary>
        /// Returns the equipped magazine.
        /// </summary>
        public abstract MagazineBehaviour GetEquippedMagazine();
        /// <summary>
        /// Returns the equipped muzzle.
        /// </summary>
        public abstract MuzzleBehaviour GetEquippedMuzzle();

        #endregion

        #endregion
    }
}
