// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Exclusive power for Medkit (Level 10): Instant Full Heal + Overheal
    /// Using medkit instantly heals to full health and provides temporary overheal shield.
    /// </summary>
    public class InstantHealPower : ExclusivePowerBehaviour
    {
        /// <summary>
        /// Amount of temporary overheal granted (additional HP beyond max).
        /// </summary>
        [SerializeField]
        private float overhealAmount = 50f;

        /// <summary>
        /// How long the overheal lasts before decaying (in seconds).
        /// </summary>
        [SerializeField]
        private float overhealDuration = 10f;

        /// <summary>
        /// Activates instant heal power.
        /// </summary>
        protected override void OnPowerActivated() {
            Debug.Log($"[InstantHealPower] Medkit upgraded! Now grants instant full heal + {overhealAmount} overheal for {overhealDuration}s");
            // Note: Actual healing logic would integrate with player health system
        }

        /// <summary>
        /// Deactivates instant heal power.
        /// </summary>
        protected override void OnPowerDeactivated() {
            Debug.Log("[InstantHealPower] Medkit returned to normal healing.");
        }
    }
}
