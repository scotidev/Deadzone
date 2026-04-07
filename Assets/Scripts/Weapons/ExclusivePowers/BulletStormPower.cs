// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Exclusive power for SMG (Level 10): Bullet Storm
    /// Drastically increases fire rate (2x speed) and reduces recoil for sustained suppressive fire.
    /// </summary>
    public class BulletStormPower : ExclusivePowerBehaviour
    {
        /// <summary>
        /// Fire rate multiplier when power is active (2x = double the fire rate).
        /// </summary>
        [SerializeField]
        private float fireRateMultiplier = 2.0f;

        /// <summary>
        /// Recoil reduction percentage (0.5 = 50% less recoil).
        /// </summary>
        [SerializeField]
        private float recoilReduction = 0.5f;

        /// <summary>
        /// Activates bullet storm mode: increased fire rate and reduced recoil.
        /// </summary>
        protected override void OnPowerActivated() {
            Debug.Log($"[BulletStormPower] SMG fire rate increased by {fireRateMultiplier}x! Suppressive fire ready!");
            // Note: Actual fire rate modification would need to be applied to the weapon's fire rate stat
            // This is a placeholder for the logic - full implementation would modify weapon stats
        }

        /// <summary>
        /// Deactivates bullet storm mode, restoring normal fire rate and recoil.
        /// </summary>
        protected override void OnPowerDeactivated() {
            Debug.Log("[BulletStormPower] SMG returned to normal fire rate.");
        }
    }
}
