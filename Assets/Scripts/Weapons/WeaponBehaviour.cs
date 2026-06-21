using UnityEngine;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Base class for weapons, implementing ItemBehaviour interface.
    /// Maintains backward compatibility with existing Weapon.cs implementations.
    /// Now inherits from ItemBehaviour for unified item selection (1-8).
    /// </summary>
    public abstract class WeaponBehaviour : ItemBehaviour {
        #region SERIALIZED FIELDS
        
        [SerializeField] protected string itemID = "1";

        #endregion
        
        #region UNITY

        protected virtual void Awake() { }

        protected virtual void Start() { }

        protected virtual void Update() { }

        protected virtual void LateUpdate() { }

        #endregion

        #region METHODS

        #region ITEM BEHAVIOUR IMPLEMENTATION

        /// <summary>
        /// Returns the item ID set in the Inspector. Must match the ID from ItemRegistry.
        /// </summary>
        public override string GetItemID() {
            return itemID;
        }

        /// <summary>
        /// Returns the weapon display name.
        /// </summary>
        public override string GetDisplayName() {
            return gameObject.name;
        }

        /// <summary>
        /// Returns the icon for this weapon.
        /// </summary>
        public override Sprite GetIcon() {
            return GetSpriteBody();
        }

        /// <summary>
        /// Called when weapon is selected. Activates the GameObject and forces initialization.
        /// </summary>
        public override void OnSelected() {
            gameObject.SetActive(true);
            Weapon weaponScript = GetComponent<Weapon>();
            if (weaponScript != null) {
                weaponScript.ForceInitialize();
            } else {
                Debug.LogWarning($"[WeaponBehaviour] Weapon component not found on {gameObject.name}!");
            }
        }

        /// <summary>
        /// Called when weapon is deselected. Deactivates the GameObject.
        /// </summary>
        public override void OnDeselected() {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Called when the player uses the weapon. Firing is handled by the Character script.
        /// </summary>
        public override void OnUse() {
        }

        /// <summary>
        /// Checks if the weapon is unlocked in PlayerProgress.
        /// </summary>
        public override bool CanBeUsed() {
            if (PlayerProgress.Instance == null) {
                Debug.LogWarning($"[{gameObject.name}] CanBeUsed: PlayerProgress.Instance is NULL! Returning false (locked).");
                return false;
            }

            string weaponID = GetItemID();
            return PlayerProgress.Instance.IsWeaponUnlocked(weaponID);
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Reloads the weapon, transferring ammo from reserve to magazine.
        /// </summary>
        public abstract void Reload();

        /// <summary>
        /// Fires the weapon with an optional spread multiplier.
        /// </summary>
        public abstract void Fire(float spreadMultiplier = 1.0f);

        /// <summary>
        /// Fills the weapon's ammunition by the given amount.
        /// </summary>
        public abstract void FillAmmunition(int amount);

        /// <summary>
        /// Ejects a casing from the weapon's ejection port.
        /// </summary>
        public abstract void EjectCasing();

        #endregion

        #region GETTERS

        /// <summary>
        /// Returns the sprite to use when displaying the weapon's body.
        /// </summary>
        public abstract Sprite GetSpriteBody();

        /// <summary>
        /// Returns the holster audio clip.
        /// </summary>
        public abstract AudioClip GetAudioClipHolster();
        /// <summary>
        /// Returns the unholster audio clip.
        /// </summary>
        public abstract AudioClip GetAudioClipUnholster();

        /// <summary>
        /// Returns the reload audio clip.
        /// </summary>
        public abstract AudioClip GetAudioClipReload();
        /// <summary>
        /// Returns the reload empty audio clip.
        /// </summary>
        public abstract AudioClip GetAudioClipReloadEmpty();

        /// <summary>
        /// Returns the fire empty audio clip.
        /// </summary>
        public abstract AudioClip GetAudioClipFireEmpty();

        /// <summary>
        /// Returns the fire audio clip.
        /// </summary>
        public abstract AudioClip GetAudioClipFire();

        /// <summary>
        /// Returns current ammunition in the magazine.
        /// </summary>
        public abstract int GetAmmunitionCurrent();
        /// <summary>
        /// Returns total ammunition capacity.
        /// </summary>
        public abstract int GetAmmunitionTotal();

        /// <summary>
        /// Returns the Weapon's Animator component.
        /// </summary>
        public abstract Animator GetAnimator();

        /// <summary>
        /// Returns true if this weapon is automatic.
        /// </summary>
        public abstract bool IsAutomatic();
        /// <summary>
        /// Returns true if the weapon has any ammunition left.
        /// </summary>
        public abstract bool HasAmmunition();

        /// <summary>
        /// Returns true if the weapon is full of ammunition.
        /// </summary>
        public abstract bool IsFull();
        /// <summary>
        /// Returns the weapon's rate of fire.
        /// </summary>
        public abstract float GetRateOfFire();

        /// <summary>
        /// Returns the RuntimeAnimatorController for this weapon.
        /// </summary>
        public abstract RuntimeAnimatorController GetAnimatorController();
        /// <summary>
        /// Returns the weapon's attachment manager.
        /// </summary>
        public abstract WeaponAttachmentManagerBehaviour GetAttachmentManager();

        #endregion

        #endregion
    }
}
