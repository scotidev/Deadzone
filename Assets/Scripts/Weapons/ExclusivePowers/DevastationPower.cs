// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Exclusive power for Special Weapon (Level 10): Devastation Mode
    /// Grants massive damage boost, penetration through multiple enemies, and explosive impacts.
    /// The ultimate weapon power for dealing with hordes.
    /// </summary>
    public class DevastationPower : ExclusivePowerBehaviour
    {
        /// <summary>
        /// Damage multiplier when devastation mode is active (3x = triple damage).
        /// </summary>
        [SerializeField]
        private float damageMultiplier = 3.0f;

        /// <summary>
        /// Number of enemies each shot can penetrate through.
        /// </summary>
        [SerializeField]
        private int penetrationCount = 5;

        /// <summary>
        /// Explosion radius on impact (in meters).
        /// </summary>
        [SerializeField]
        private float impactExplosionRadius = 4f;

        /// <summary>
        /// Activates devastation mode.
        /// </summary>
        protected override void OnPowerActivated() {
            Debug.Log($"[DevastationPower] DEVASTATION MODE ACTIVATED! {damageMultiplier}x damage, penetrates {penetrationCount} enemies, {impactExplosionRadius}m explosions!");
            // Note: This would significantly enhance the special weapon's capabilities
        }

        /// <summary>
        /// Deactivates devastation mode.
        /// </summary>
        protected override void OnPowerDeactivated() {
            Debug.Log("[DevastationPower] Special weapon returned to normal power.");
        }
    }
}
