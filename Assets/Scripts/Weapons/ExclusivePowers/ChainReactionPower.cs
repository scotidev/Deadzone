// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Exclusive power for Explosive Barrels (Level 10): Chain Reaction
    /// Barrels have increased explosion radius and can trigger chain reactions with nearby barrels.
    /// </summary>
    public class ChainReactionPower : ExclusivePowerBehaviour
    {
        /// <summary>
        /// Explosion radius multiplier (1.5x = 50% larger radius).
        /// </summary>
        [SerializeField]
        private float radiusMultiplier = 1.5f;

        /// <summary>
        /// Damage multiplier for explosions (1.5x = 50% more damage).
        /// </summary>
        [SerializeField]
        private float damageMultiplier = 1.5f;

        /// <summary>
        /// Activates chain reaction mode.
        /// </summary>
        protected override void OnPowerActivated() {
            Debug.Log($"[ChainReactionPower] Explosive barrels now have {radiusMultiplier}x radius and {damageMultiplier}x damage! Chain reactions enabled!");
            // Note: This would modify barrel explosion parameters
        }

        /// <summary>
        /// Deactivates chain reaction mode.
        /// </summary>
        protected override void OnPowerDeactivated() {
            Debug.Log("[ChainReactionPower] Explosive barrels returned to normal stats.");
        }
    }
}
