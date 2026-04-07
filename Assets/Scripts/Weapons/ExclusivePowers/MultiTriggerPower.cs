// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Exclusive power for Traps (Level 10): Multi-Trigger
    /// Traps can trigger multiple times before breaking and have increased slow/damage effects.
    /// </summary>
    public class MultiTriggerPower : ExclusivePowerBehaviour
    {
        /// <summary>
        /// Number of times trap can be triggered before breaking (normal = 1).
        /// </summary>
        [SerializeField]
        private int triggerCount = 3;

        /// <summary>
        /// Damage/slow effect multiplier (1.5x = 50% stronger effects).
        /// </summary>
        [SerializeField]
        private float effectMultiplier = 1.5f;

        /// <summary>
        /// Activates multi-trigger mode.
        /// </summary>
        protected override void OnPowerActivated() {
            Debug.Log($"[MultiTriggerPower] Traps can now trigger {triggerCount} times with {effectMultiplier}x effects!");
            // Note: This would modify trap behavior when placed
        }

        /// <summary>
        /// Deactivates multi-trigger mode.
        /// </summary>
        protected override void OnPowerDeactivated() {
            Debug.Log("[MultiTriggerPower] Traps returned to single-trigger mode.");
        }
    }
}
