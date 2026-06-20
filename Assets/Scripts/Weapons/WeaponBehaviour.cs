// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Base class for weapons, implementing ItemBehaviour interface.
    /// Maintains backward compatibility with existing Weapon.cs implementations.
    /// REFATORAÇÃO: WeaponBehaviour agora herda de ItemBehaviour, permitindo
    /// que armas façam parte do sistema unificado de seleção de items (1-8).
    /// </summary>
    public abstract class WeaponBehaviour : ItemBehaviour {
        #region SERIALIZED FIELDS
        
        [SerializeField] protected string itemID = "1"; // Must be set to "1", "2", or "3" in Inspector

        #endregion
        
        #region UNITY

        protected virtual void Awake() { }

        protected virtual void Start() { }

        protected virtual void Update() { }

        protected virtual void LateUpdate() { }

        #endregion

        #region ITEM BEHAVIOUR IMPLEMENTATION

        /// <summary>
        /// Returns the item ID set in the Inspector. Must match the ID from ItemRegistry.
        /// CONCEITO: O ID aqui deve ser configurado manualmente no Inspector para cada arma (1, 2, 3).
        /// Este ID é usado para fazer lookup no PlayerProgress e verificar se a arma está desbloqueada.
        /// </summary>
        public override string GetItemID() {
            return itemID;
        }

        /// <summary>
        /// Default implementation. Returns weapon name from GetSpriteBody or object name.
        /// </summary>
        public override string GetDisplayName() {
            return gameObject.name;
        }

        /// <summary>
        /// Default implementation. Returns sprite body as icon.
        /// </summary>
        public override Sprite GetIcon() {
            return GetSpriteBody();
        }

        /// <summary>
        /// Called when weapon is selected (key pressed).
        /// Activates the weapon GameObject and forces initialization.
        /// </summary>
        public override void OnSelected() {
            Debug.Log($"[WeaponBehaviour] OnSelected called for {GetDisplayName()}");
            
            // CONCEITO: Ao selecionar uma arma, ativamos o GameObject dela
            // que contém todos os components necessários (Animator, Magazine, etc).
            // Isso segue o padrão do Infima Games de usar GameObjects filhos.
            gameObject.SetActive(true);
            Debug.Log($"[WeaponBehaviour] GameObject activated: {gameObject.name}");
            
            // SINCRONIZAÇÃO: Se Start() ainda não foi chamado, chamamos manualmente
            // para garantir que magazineBehaviour e muzzleBehaviour estejam inicializados.
            // Isso previne NullReferenceException no primeiro frame de ativação.
            Weapon weaponScript = GetComponent<Weapon>();
            if (weaponScript != null) {
                Debug.Log($"[WeaponBehaviour] Found Weapon component, calling ForceInitialize()");
                weaponScript.ForceInitialize();
            } else {
                Debug.LogWarning($"[WeaponBehaviour] Weapon component not found on {gameObject.name}!");
            }
        }

        /// <summary>
        /// Called when weapon is deselected (another item selected).
        /// Deactivates the weapon GameObject.
        /// </summary>
        public override void OnDeselected() {
            // CONCEITO: Ao deselecionar, desativamos o GameObject para que
            // outro item possa ser selecionado. Isso economiza processamento.
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Called when player uses the weapon (fire button).
        /// For weapons, fire is handled by Character script via input system.
        /// This method is kept for interface compliance.
        /// </summary>
        public override void OnUse() {
            // Fire button is handled by Character script, not here
            // Weapon firing is driven by Character input handling
        }

        /// <summary>
        /// Check if weapon can be used (only checks if unlocked).
        /// Note: Ammo check happens during fire, not selection. You can select a weapon with no ammo.
        /// </summary>
        public override bool CanBeUsed() {
            // CONCEITO: Verificamos APENAS se a arma está desbloqueada.
            // Ammo é checado durante o fire, não durante seleção.
            // Isso permite que o player selecione qualquer arma desbloqueada, mesmo sem munição.
            if (PlayerProgress.Instance == null) {
                Debug.LogWarning($"[{gameObject.name}] CanBeUsed: PlayerProgress.Instance is NULL! Returning false (locked).");
                return false; // SEGURANÇA: retorna false ao invés de true para evitar arma liberada
                              // antes do PlayerProgress ser inicializado (bug WebGL em StreetMap/DesertMap)
            }

            string weaponID = GetItemID();
            
            // Verificamos APENAS se a arma está desbloqueada no PlayerProgress.
            // Removido o bypass ID == "1" para suportar o fluxo de tutorial onde a Pistola começa bloqueada.
            bool isUnlocked = PlayerProgress.Instance.IsWeaponUnlocked(weaponID);
            Debug.Log($"[{gameObject.name}] CanBeUsed check: ID={weaponID}, Unlocked={isUnlocked}");
            
            return isUnlocked;
        }

        #endregion

        #region GETTERS

        /// <summary>
        /// Returns the sprite to use when displaying the weapon's body.
        /// </summary>
        /// <returns></returns>
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
        /// Returns Current Ammunition. 
        /// </summary>
        public abstract int GetAmmunitionCurrent();
        /// <summary>
        /// Returns Total Ammunition.
        /// </summary>
        public abstract int GetAmmunitionTotal();

        /// <summary>
        /// Returns the Weapon's Animator component.
        /// </summary>
        public abstract Animator GetAnimator();

        /// <summary>
        /// Returns true if this weapon shoots in automatic.
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
        /// Returns the RuntimeAnimationController the Character needs to use when this Weapon is equipped!
        /// </summary>
        public abstract RuntimeAnimatorController GetAnimatorController();
        /// <summary>
        /// Returns the weapon's attachment manager component.
        /// </summary>
        public abstract WeaponAttachmentManagerBehaviour GetAttachmentManager();

        #endregion

        #region METHODS

        /// <summary>
        /// Fires the weapon.
        /// </summary>
        /// <param name="spreadMultiplier">Value to multiply the weapon's spread by. Very helpful to account for aimed spread multipliers.</param>
        public abstract void Fire(float spreadMultiplier = 1.0f);
        /// <summary>
        /// Reloads the weapon.
        /// </summary>
        public abstract void Reload();

        /// <summary>
        /// Fills the character's equipped weapon's ammunition by a certain amount, or fully if set to -1.
        /// </summary>
        public abstract void FillAmmunition(int amount);

        /// <summary>
        /// Ejects a casing from the weapon. This is commonly called from animation events, but can be called from anywhere.
        /// </summary>
        public abstract void EjectCasing();

        #endregion
    }
}