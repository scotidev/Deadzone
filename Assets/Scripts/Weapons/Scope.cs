using System;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Weapon Scope. Handles scope appearance via sprite for the character's interface.
    /// </summary>
    public class Scope : ScopeBehaviour {
        #region SERIALIZED FIELDS

        [Header("Interface")]

        [SerializeField] private Sprite sprite;

        #endregion

        #region METHODS

        #region GETTERS

        public override Sprite GetSprite() => sprite;

        #endregion

        #endregion
    }
}
