using UnityEngine;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Character Abstract Behaviour.
    /// </summary>
    public abstract class CharacterBehaviour : MonoBehaviour {
        #region UNITY

        protected virtual void Awake() { }

        protected virtual void Start() { }

        protected virtual void Update() { }

        protected virtual void LateUpdate() { }

        #endregion

        #region METHODS

        #region GETTERS

        /// <summary>
        /// Returns the player character's main camera.
        /// </summary>
        public abstract Camera GetCameraWorld();

        /// <summary>
        /// Returns a reference to the Inventory component.
        /// </summary>
        public abstract InventoryBehaviour GetInventory();

        /// <summary>
        /// Returns true if the character is interacting with a menu or shop.
        /// </summary>
        public abstract bool IsInterfaceMode();

        /// <summary>
        /// Defines if the character should enter or exit interface mode. This locks actions such as shooting and moving the camera.
        /// </summary>
        public abstract void SetInterfaceMode(bool value);

        /// <summary>
        /// Holsters or unholsters the character's weapon. This can be used to trigger a holster animation, or simply hide the weapon model.
        /// </summary>
        public abstract void SetHolstered(bool value);

        /// <summary>
        /// Returns true if the character is currently performing a melee attack.
        /// </summary>
        public abstract bool IsAttackingMelee();

        /// <summary>
        /// Returns true if the Crosshair should be visible.
        /// </summary>
        public abstract bool IsCrosshairVisible();

        /// <summary>
        /// Returns true if the character is running.
        /// </summary>
        public abstract bool IsRunning();

        /// <summary>
        /// Returns true if the character is aiming.
        /// </summary>
        public abstract bool IsAiming();

        /// <summary>
        /// Returns true if the game cursor is locked.
        /// </summary>
        public abstract bool IsCursorLocked();

        /// <summary>
        /// Returns true if the tutorial text should be visible on the screen.
        /// </summary>
        public abstract bool IsTutorialTextVisible();

        /// <summary>
        /// Returns the Movement Input.
        /// </summary>
        public abstract Vector2 GetInputMovement();

        /// <summary>
        /// Returns the Look Input.
        /// </summary>
        public abstract Vector2 GetInputLook();

        /// <summary>
        /// Returns true if the player is pressing the button to jump.
        /// </summary>
        public abstract bool IsJumping();

        /// <summary>
        /// Returns true if the character is crouching.
        /// </summary>
        public abstract bool IsCrouching();

        #endregion

        #region ANIMATION

        /// <summary>
        /// Ejects a casing from the equipped weapon.
        /// </summary>
        public abstract void EjectCasing();

        /// <summary>
        /// Fills the character's equipped weapon's ammunition by a certain amount, or fully if set to -1.
        /// </summary>
        public abstract void FillAmmunition(int amount);

        /// <summary>
        /// Sets the equipped weapon's magazine to be active or inactive!
        /// </summary>
        public abstract void SetActiveMagazine(int active);

        /// <summary>
        /// Reload Animation Ended.
        /// </summary>
        public abstract void AnimationEndedReload();

        /// <summary>
        /// Inspect Animation Ended.
        /// </summary>
        public abstract void AnimationEndedInspect();

        /// <summary>
        /// Holster Animation Ended.
        /// </summary>
        public abstract void AnimationEndedHolster();

        #endregion

        #endregion
    }
}
