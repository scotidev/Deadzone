using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Magazine Behaviour. Abstract base class defining magazine properties
    /// such as ammunition total and UI sprite.
    /// </summary>
    public abstract class MagazineBehaviour : MonoBehaviour
    {
        #region METHODS
        
        #region GETTERS
        
        /// <summary>
        /// Returns The Total Ammunition.
        /// </summary>
        public abstract int GetAmmunitionTotal();
        /// <summary>
        /// Returns the Sprite used on the Character's Interface.
        /// </summary>
        public abstract Sprite GetSprite();

        #endregion

        #endregion
    }
}
