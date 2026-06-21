using UnityEngine;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Handles all the animation events that come from the character in the asset.
    /// </summary>
    public class CharacterAnimationEventHandler : MonoBehaviour {

        #region FIELDS

        private CharacterBehaviour playerCharacter;

        #endregion

        #region UNITY

        private void Awake() {
            playerCharacter = ServiceLocator.Current.Get<IGameModeService>().GetPlayerCharacter();
        }

        #endregion

        #region METHODS

        #region ANIMATION

        /// <summary>
        /// Ejects a casing from the character's equipped weapon. This function is called from an Animation Event.
        /// </summary>
        private void OnEjectCasing() {
            if (playerCharacter != null)
                playerCharacter.EjectCasing();
        }

        /// <summary>
        /// Fills the character's equipped weapon's ammunition by a certain amount, or fully if set to 0. This function is called from a Animation Event.
        /// </summary>
        private void OnAmmunitionFill(int amount = 0) {
            if (playerCharacter != null)
                playerCharacter.FillAmmunition(amount);
        }
        /// <summary>
        /// Sets the character's knife active value. This function is called from an Animation Event.
        /// </summary>
        private void OnSetActiveKnife(int active) {
        }

        /// <summary>
        /// Spawns a grenade at the correct location. This function is called from an Animation Event.
        /// </summary>
        private void OnGrenade() {
        }
        /// <summary>
        /// Sets the equipped weapon's magazine to be active or inactive! This function is called from an Animation Event.
        /// </summary>
        private void OnSetActiveMagazine(int active) {
            if (playerCharacter != null)
                playerCharacter.SetActiveMagazine(active);
        }

        /// <summary>
        /// Bolt Animation Ended. This function is called from an Animation Event.
        /// </summary>
        private void OnAnimationEndedBolt() {
        }
        /// <summary>
        /// Reload Animation Ended. This function is called from an Animation Event.
        /// </summary>
        private void OnAnimationEndedReload() {
            if (playerCharacter != null)
                playerCharacter.AnimationEndedReload();
        }

        /// <summary>
        /// Grenade Throw Animation Ended. This function is called from an Animation Event.
        /// </summary>
        private void OnAnimationEndedGrenadeThrow() {
        }
        /// <summary>
        /// Melee Animation Ended. This function is called from an Animation Event.
        /// </summary>
        private void OnAnimationEndedMelee() {
            if (playerCharacter != null && playerCharacter is Character character) {
                character.EndMeleeAttack();
            }
        }

        /// <summary>
        /// Inspect Animation Ended. This function is called from an Animation Event.
        /// </summary>
        private void OnAnimationEndedInspect() {
            if (playerCharacter != null)
                playerCharacter.AnimationEndedInspect();
        }
        /// <summary>
        /// Holster Animation Ended. This function is called from an Animation Event.
        /// </summary>
        private void OnAnimationEndedHolster() {
            if (playerCharacter != null)
                playerCharacter.AnimationEndedHolster();
        }

        /// <summary>
        /// Sets the character's equipped weapon's slide back pose. This function is called from an Animation Event.
        /// </summary>
        private void OnSlideBack(int back) {
        }

        #endregion

        #endregion
    }
}
