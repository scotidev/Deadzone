// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Exclusive power for Shotgun (Level 10): Explosive Shells
    /// Each shot creates a small explosion on impact, dealing area damage to nearby enemies.
    /// </summary>
    public class ExplosiveShellsPower : ExclusivePowerBehaviour
    {
        /// <summary>
        /// Explosion radius in meters.
        /// </summary>
        [SerializeField]
        private float explosionRadius = 3.0f;

        /// <summary>
        /// Bonus explosion damage added to the shot.
        /// </summary>
        [SerializeField]
        private float explosionDamage = 50f;

        /// <summary>
        /// Optional explosion effect prefab.
        /// </summary>
        [SerializeField]
        private GameObject explosionEffectPrefab;

        /// <summary>
        /// Activates explosive shells mode.
        /// </summary>
        protected override void OnPowerActivated() {
            Debug.Log($"[ExplosiveShellsPower] Shotgun shells now explode on impact! Radius: {explosionRadius}m, Bonus Damage: {explosionDamage}");
            // Note: Actual explosion logic would be integrated with the weapon's hit detection
            // When a projectile hits, it would trigger an explosion at the impact point
        }

        /// <summary>
        /// Deactivates explosive shells mode.
        /// </summary>
        protected override void OnPowerDeactivated() {
            Debug.Log("[ExplosiveShellsPower] Shotgun returned to normal shells.");
        }

        /// <summary>
        /// This method would be called by the weapon when it hits a target.
        /// Creates an explosion at the specified position.
        /// </summary>
        public void TriggerExplosion(Vector3 impactPosition) {
            if (!isActive) return;

            // Spawn explosion effect
            if (explosionEffectPrefab != null) {
                Instantiate(explosionEffectPrefab, impactPosition, Quaternion.identity);
            }

            // Find all enemies in radius and apply explosion damage
            Collider[] hitColliders = Physics.OverlapSphere(impactPosition, explosionRadius);
            foreach (Collider col in hitColliders) {
                EnemyBase enemy = col.GetComponent<EnemyBase>();
                if (enemy != null) {
                    enemy.TakeDamage(explosionDamage);
                }
            }

            Debug.Log($"[ExplosiveShellsPower] Explosion triggered at {impactPosition}");
        }
    }
}
