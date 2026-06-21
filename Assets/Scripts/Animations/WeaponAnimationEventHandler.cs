// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Handles all the animation events that come from the weapon in the asset.
    /// </summary>
    public class WeaponAnimationEventHandler : MonoBehaviour {
        #region FIELDS

        private WeaponBehaviour weapon;

        #endregion

        #region UNITY

        private void Awake() {
            weapon = GetComponent<WeaponBehaviour>();
        }

        #endregion

        #region ANIMATION

        /// <summary>
        /// Called by the animation clip to eject a casing from the weapon.
        /// </summary>
        private void OnEjectCasing() {
            if (weapon != null)
                weapon.EjectCasing();
        }

        #endregion
    }
}
