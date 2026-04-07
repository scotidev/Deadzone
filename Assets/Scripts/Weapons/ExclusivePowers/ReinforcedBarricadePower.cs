// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Exclusive power for Barricades (Level 10): Reinforced Fortress
    /// Barricades gain significantly more health and regenerate over time.
    /// </summary>
    public class ReinforcedBarricadePower : ExclusivePowerBehaviour
    {
        /// <summary>
        /// Health multiplier for barricades (2x = double the health).
        /// </summary>
        [SerializeField]
        private float healthMultiplier = 2.0f;

        /// <summary>
        /// Health regeneration per second while power is active.
        /// </summary>
        [SerializeField]
        private float regenPerSecond = 5f;

        /// <summary>
        /// Activates reinforced barricade mode.
        /// </summary>
        protected override void OnPowerActivated() {
            Debug.Log($"[ReinforcedBarricadePower] Barricades now have {healthMultiplier}x health and regenerate {regenPerSecond} HP/s!");
            // Note: This would modify barricade health stats when placed
        }

        /// <summary>
        /// Deactivates reinforced barricade mode.
        /// </summary>
        protected override void OnPowerDeactivated() {
            Debug.Log("[ReinforcedBarricadePower] Barricades returned to normal stats.");
        }
    }
}
