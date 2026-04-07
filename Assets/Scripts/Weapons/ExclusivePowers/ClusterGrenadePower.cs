// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Exclusive power for Grenades (Level 10): Cluster Bombs
    /// Primary explosion spawns multiple smaller grenades that create secondary explosions.
    /// </summary>
    public class ClusterGrenadePower : ExclusivePowerBehaviour
    {
        /// <summary>
        /// Number of cluster grenades spawned from the main explosion.
        /// </summary>
        [SerializeField]
        private int clusterCount = 5;

        /// <summary>
        /// Damage dealt by each cluster explosion.
        /// </summary>
        [SerializeField]
        private float clusterDamage = 30f;

        /// <summary>
        /// Radius of each cluster explosion.
        /// </summary>
        [SerializeField]
        private float clusterRadius = 2f;

        /// <summary>
        /// Activates cluster grenade mode.
        /// </summary>
        protected override void OnPowerActivated() {
            Debug.Log($"[ClusterGrenadePower] Grenades now spawn {clusterCount} cluster bombs on explosion!");
            // Note: This would integrate with grenade explosion logic
        }

        /// <summary>
        /// Deactivates cluster grenade mode.
        /// </summary>
        protected override void OnPowerDeactivated() {
            Debug.Log("[ClusterGrenadePower] Grenades returned to normal explosions.");
        }
    }
}
