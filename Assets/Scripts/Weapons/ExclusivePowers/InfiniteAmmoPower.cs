// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Exclusive power for Pistol (Level 10): Infinite Ammo
    /// Never needs to reload, magazine never depletes.
    /// NOTE: This is a placeholder implementation. Full integration requires
    /// modifying the Weapon.cs Fire() method to skip ammo consumption when this power is active.
    /// </summary>
    public class InfiniteAmmoPower : ExclusivePowerBehaviour
    {
        /// <summary>
        /// When activated, log the infinite ammo status.
        /// Actual implementation would modify Weapon.cs to skip ammunition consumption.
        /// </summary>
        protected override void OnPowerActivated() {
            Debug.Log("[InfiniteAmmoPower] Pistol now has infinite ammo! (Full implementation requires Weapon.cs modification)");
            // TODO: In a full implementation, this would hook into Weapon.Fire() to prevent ammo consumption
            // For now, this serves as a marker that the power is active
        }

        /// <summary>
        /// Restore original ammo behavior if power is deactivated.
        /// </summary>
        protected override void OnPowerDeactivated() {
            Debug.Log("[InfiniteAmmoPower] Pistol ammo restored to normal.");
        }
    }
}
