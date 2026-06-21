using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Scope Behaviour. Abstract base class defining scope properties
    /// such as the UI sprite for the scope reticle.
    /// </summary>
    public abstract class ScopeBehaviour : MonoBehaviour
    {
        #region METHODS

        #region GETTERS

        /// <summary>
        /// Returns the Sprite used on the Character's Interface.
        /// </summary>
        public abstract Sprite GetSprite();

        #endregion

        #endregion
    }
}
