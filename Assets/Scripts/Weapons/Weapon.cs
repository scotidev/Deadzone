// Copyright 2021, Infima Games. All Rights Reserved.

using System.Collections;
using UnityEngine;

// refatoração: o sistema de fire rate respeita o roundsperminute desse script? precisamos centralizar essa logica, nao podemos ter ela duplicada no projeto, verifique os SO de weapon e em outro lugar pra descobrir se temos uma lógica de manter o fire rate em algum outro lugar. Os stats das armas poderão ser atualizados conforme upgrades, e sei que temos pelo menos um script (UpgradeManager) que está envolvido nisso, precisamos dar upgrade nas coisas mas de forma eficiente e consistente.

// REFATORAÇÃO: tbm quero remover a possibilidade (feature) de dar holster nas armas, mas cuidado: sei que em algum lugar do projecto, acredito que em character, mas pode ter mais lugares, em que o holster tambem faz parte da logica de trocar de arma, para principalmente usar buildables, entao importante verificar o que pode acontecer antes de refatorar, analise profunda

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Weapon. This class handles most of the things that weapons need.
    /// </summary>
    public class Weapon : WeaponBehaviour {

        #region SERIALIZED FIELDS

        [Header("Firing")]

        [Tooltip("The ScriptableObject containing all base stats and scaling for this weapon. Mandatory for the weapon to fire correctly.")]
        [SerializeField] private WeaponDataSO weaponData;
        [SerializeField] private float projectileImpulse = 400.0f;

        [Tooltip("Mask of things recognized when firing.")]
        [SerializeField] private LayerMask mask;

        [SerializeField] private float maximumDistance = 5000.0f;

        [Header("Animation")]

        [Tooltip("Transform that represents the weapon's ejection port, meaning the part of the weapon that casings shoot from.")]
        [SerializeField] private Transform socketEjection;
        [SerializeField] private GameObject prefabCasing;
        [SerializeField] private GameObject prefabProjectile;
        [SerializeField] public RuntimeAnimatorController controller;
        [SerializeField] private Sprite spriteBody;

        [Header("Audio Clips Holster")]

        [SerializeField] private AudioClip audioClipHolster;
        [SerializeField] private AudioClip audioClipUnholster;
        [SerializeField] private AudioClip audioClipReload;
        [SerializeField] private AudioClip audioClipReloadEmpty;
        [SerializeField] private AudioClip audioClipFireEmpty;

        #endregion

        #region FIELDS

        private Animator animator;
        private WeaponAttachmentManagerBehaviour attachmentManager;
        private IGameModeService gameModeService;
        private CharacterBehaviour characterBehaviour;
        private Transform playerCamera;
        private int ammunitionCurrent;
        // LEGACY: Kept for ForceInitialize guard only. The persistent initialization
        // flag is now stored in PlayerProgress.IsAmmoInitialized() to survive weapon cloning.
        private bool hasInitializedAmmo;

        private MagazineBehaviour magazineBehaviour;
        private MuzzleBehaviour muzzleBehaviour;

        private float currentDamage;
        private float currentFireRate;
        private bool isAutomatic;
        private IAudioManagerService audioService;

        #endregion

        #region UNITY

        protected override void Awake() {
            animator = GetComponent<Animator>();
            attachmentManager = GetComponent<WeaponAttachmentManagerBehaviour>();

            gameModeService = ServiceLocator.Current.Get<IGameModeService>();
            characterBehaviour = gameModeService.GetPlayerCharacter();
            playerCamera = characterBehaviour.GetCameraWorld().transform;

            audioService = ServiceLocator.Current.Get<IAudioManagerService>();

            if (weaponData == null) {
                Debug.LogError($"[Weapon] {gameObject.name} (ID: {itemID}) HAS NO WEAPONDATASO ASSIGNED! Assign a WeaponDataSO in the Inspector.", this);
            }

            // Subscribe to upgrade events to refresh stats when this weapon is upgraded
            UpgradeManager.OnItemUpgraded += HandleItemUpgraded;

            // CONCEITO: Pré-cria 10 projéteis no pool para evitar Instantiate
            // nos primeiros tiros. O GameObjectPool gerencia tudo internamente:
            // cria os objetos, adiciona PooledObject e os mantém inativos até serem usados.
            if (prefabProjectile != null) {
                GameObjectPool.Prewarm(prefabProjectile, 10);
                useProjectilePool = true;
            }
        }

        private void OnDestroy() {
            // Unsubscribe to avoid memory leaks
            UpgradeManager.OnItemUpgraded -= HandleItemUpgraded;
        }
        
        protected override void Start() {
            RefreshStats();
            InitializeWeapon();
        }

        private void HandleItemUpgraded(string upgradedItemID, ItemDataSO itemData) {
            if (upgradedItemID == itemID) {
                Debug.Log($"[Weapon] Item {itemID} upgraded! Refreshing stats and ammo.");
                RefreshStats();
                
                // Sync ammo from PlayerProgress (which was just filled to max by FillItemToMax)
                if (PlayerProgress.Instance != null) {
                    ammunitionCurrent = PlayerProgress.Instance.GetItemCurrent(itemID);
                }
            }
        }

        /// <summary>
        /// Refreshes the weapon's stats based on the WeaponDataSO and current upgrade level.
        /// This centralizes the logic to ensure stats are always up to date.
        /// </summary>
        public void RefreshStats() {
            if (weaponData == null) {
                // Emergency fallbacks to prevent division by zero or crashes if SO is missing
                currentDamage = 1f;
                currentFireRate = 1f;
                isAutomatic = false;
                return;
            }

            // The weaponData's itemID should match the weapon's itemID
            if (weaponData.ItemID != itemID) {
                Debug.LogWarning($"[Weapon] {gameObject.name} itemID mismatch! Script itemID: {itemID}, SO itemID: {weaponData.ItemID}. Stats will scale based on weapon's itemID: {itemID}");
            }

            int level = PlayerProgress.Instance != null ? PlayerProgress.Instance.GetItemLevel(itemID) : 1;
            
            currentDamage = weaponData.GetDamageAtLevel(level);
            currentFireRate = weaponData.GetFireRateAtLevel(level);
            isAutomatic = weaponData.isAutomatic;

            // SAFETY: Ensure fire rate is never 0 to avoid division by zero in Character shot timing logic
            if (currentFireRate <= 0) {
                currentFireRate = 1;
                Debug.LogWarning($"[Weapon] {itemID} has 0 fire rate in SO! Using 1 to avoid crash.");
            }

            Debug.Log($"[Weapon] {itemID} Stats Refreshed (Level {level}): Damage={currentDamage}, FireRate={currentFireRate}, Auto={isAutomatic}");
        }

        /// <summary>
        /// Initialize weapon components (magazine, muzzle, ammo).
        /// Called both by Start() and by OnSelected() to ensure initialization.
        /// </summary>
        public void ForceInitialize() {
            Debug.Log($"[Weapon] ForceInitialize: hasInitializedAmmo={hasInitializedAmmo}, magazine={magazineBehaviour != null}, muzzle={muzzleBehaviour != null}");

            // Ensure stats are refreshed whenever we force initialize
            RefreshStats();

            // FIXED: Use hasInitializedAmmo as additional guard to prevent re-initializing
            // ammo values (current and total) after the weapon was already set up.
            // This prevents ForceInitialize from resetting ammo to max when called
            // from WeaponBehaviour.OnSelected() on re-selection (e.g. after placing a buildable).
            if (hasInitializedAmmo && magazineBehaviour != null && muzzleBehaviour != null) {
                Debug.Log($"[Weapon] ForceInitialize: Already initialized, skipping");
                return;
            }
            
            Debug.Log($"[Weapon] ForceInitialize: Starting initialization");
            InitializeWeapon();
        }

        private void InitializeWeapon() {
            int ammoBeforeInit = PlayerProgress.Instance != null ? PlayerProgress.Instance.GetItemCurrent(itemID) : -1;
            int totalBeforeInit = PlayerProgress.Instance != null ? PlayerProgress.Instance.GetItemTotal(itemID) : -1;
            Debug.Log($"[Weapon] InitializeWeapon START: itemID={itemID}, hasInitializedAmmo={hasInitializedAmmo}, ammoBefore={ammoBeforeInit}, totalBefore={totalBeforeInit}, attachmentManager={attachmentManager}");

            if (attachmentManager == null) {
                Debug.LogWarning($"[Weapon] InitializeWeapon: attachmentManager is NULL!");
                return;
            }

            magazineBehaviour = attachmentManager.GetEquippedMagazine();
            muzzleBehaviour = attachmentManager.GetEquippedMuzzle();

            Debug.Log($"[Weapon] InitializeWeapon: magazineBehaviour={magazineBehaviour}, muzzleBehaviour={muzzleBehaviour}");

            if (magazineBehaviour != null) {
                if (PlayerProgress.Instance != null) {
                    int maxCurrent = magazineBehaviour.GetAmmunitionTotal();
                    
                    // FIXED: Use persistent IsAmmoInitialized() from PlayerProgress instead of
                    // per-instance hasInitializedAmmo. This prevents cloned weapons from
                    // re-initializing ammo (resetting total to max).
                    if (!PlayerProgress.Instance.IsAmmoInitialized(itemID)) {
                        // First time this WEAPON TYPE is initialized.
                        // Give starting ammo: full magazine + one reserve.
                        PlayerProgress.Instance.SetItemCurrent(itemID, maxCurrent);
                        Debug.Log($"[Weapon] SetItemCurrent({itemID}, {maxCurrent}): first init, ammo was {ammoBeforeInit}");
                        
                        int currentTotal = PlayerProgress.Instance.GetItemTotal(itemID);
                        if (currentTotal <= 0) {
                            PlayerProgress.Instance.SetItemTotal(itemID, maxCurrent);
                            Debug.Log($"[Weapon] FIRST INIT: SetItemTotal({itemID}, {maxCurrent}) - starting reserve ammo");
                        } else {
                            Debug.Log($"[Weapon] FIRST INIT: total already {currentTotal}, NOT overriding");
                        }
                        PlayerProgress.Instance.MarkAmmoInitialized(itemID);
                        ammunitionCurrent = maxCurrent;
                    } else {
                        // Already initialized - sync from PlayerProgress (read-only).
                        // This ensures cloned weapons don't reset current to max,
                        // and weapons remember their actual magazine state.
                        ammunitionCurrent = PlayerProgress.Instance.GetItemCurrent(itemID);
                        Debug.Log($"[Weapon] Already initialized: local ammo synced from PP={ammunitionCurrent}, maxCurrent={maxCurrent}");
                    }
                    
                    hasInitializedAmmo = true;
                } else {
                    ammunitionCurrent = magazineBehaviour.GetAmmunitionTotal();
                }
                
                Debug.Log($"[Weapon] Final ammunitionCurrent = {ammunitionCurrent}");
            } else {
                Debug.LogWarning($"[Weapon] InitializeWeapon: magazineBehaviour is NULL after attachment lookup!");
            }

            if (muzzleBehaviour == null) {
                Debug.LogWarning($"[Weapon] InitializeWeapon: muzzleBehaviour is NULL after attachment lookup!");
            }
        }

        #endregion

        #region GETTERS

        public override Animator GetAnimator() => animator;

        public override Sprite GetSpriteBody() => spriteBody;

        public override AudioClip GetAudioClipHolster() => audioClipHolster;
        public override AudioClip GetAudioClipUnholster() => audioClipUnholster;

        public override AudioClip GetAudioClipReload() => audioClipReload;
        public override AudioClip GetAudioClipReloadEmpty() => audioClipReloadEmpty;

        public override AudioClip GetAudioClipFireEmpty() => audioClipFireEmpty;

        public override AudioClip GetAudioClipFire() => muzzleBehaviour != null ? muzzleBehaviour.GetAudioClipFire() : null;

        public override int GetAmmunitionCurrent() => ammunitionCurrent;

        /// <summary>
        /// Get total ammunition in magazine. Safely handles null magazineBehaviour.
        /// CONCEITO: Se magazineBehaviour for null, significa que o Weapon ainda não foi inicializado corretamente.
        /// Retornamos 0 para evitar NullReferenceException.
        /// </summary>
        public override int GetAmmunitionTotal() {
            if (magazineBehaviour == null) {
                Debug.LogWarning($"[Weapon] GetAmmunitionTotal called but magazineBehaviour is NULL! Returning 0.");
                return 0;
            }
            return magazineBehaviour.GetAmmunitionTotal();
        }

        public override bool IsAutomatic() => isAutomatic;
        public override float GetRateOfFire() => currentFireRate;

        /// <summary>
        /// Check if magazine is full. Safely handles null magazineBehaviour.
        /// </summary>
        public override bool IsFull() {
            if (magazineBehaviour == null) return false;
            return ammunitionCurrent == magazineBehaviour.GetAmmunitionTotal();
        }
        
        public override bool HasAmmunition() => ammunitionCurrent > 0;

        public override RuntimeAnimatorController GetAnimatorController() => controller;
        public override WeaponAttachmentManagerBehaviour GetAttachmentManager() => attachmentManager;

        #endregion

        #region METHODS

        /// <summary>
        /// Reload the weapon by transferring ammo from inventory to magazine.
        /// Uses PlayerProgress.ReloadItem() which handles the transfer logic.
        /// </summary>
        public override void Reload() {
            Debug.Log($"[Weapon] Reload START: itemID={itemID}, ammunitionCurrent={ammunitionCurrent}, IsFull={IsFull()}, hasInitializedAmmo={hasInitializedAmmo}");
            
            // FIXED: Check if magazine is already full - no need to reload.
            if (IsFull()) {
                Debug.Log($"[Weapon] Reload called but magazine is already full.");
                return;
            }

            // FIXED: Check if there's reserve ammo available.
            // If no reserve and magazine isn't full, play empty-click sound and skip animation.
            if (PlayerProgress.Instance != null) {
                int total = PlayerProgress.Instance.GetItemTotal(itemID);
                int current = PlayerProgress.Instance.GetItemCurrent(itemID);
                int maxCurrent = PlayerProgress.Instance.GetItemMaxCurrent(itemID);
                Debug.Log($"[Weapon] Reload check: total={total}, current={current}, maxCurrent={maxCurrent}");
                if (total <= 0) {
                    // No reserve ammo - play a feedback sound instead of reload animation.
                    // This gives the player clear audio feedback that they're out of ammo.
                    if (audioClipFireEmpty != null) {
                        audioService?.PlaySFX3D(audioClipFireEmpty, transform.position, 1f);
                    }
                    Debug.Log($"[Weapon] Cannot reload {itemID} - no reserve ammo available.");
                    return;
                }
            }

            // CONCEITO: Reload animation depends on whether magazine is empty or not
            animator.Play(HasAmmunition() ? "Reload" : "Reload Empty", 0, 0.0f);

            // NEW: Use PlayerProgress to handle reload (transfer from total to current)
            // This respects the weapon's max magazine capacity based on upgrades
            if (PlayerProgress.Instance != null) {
                PlayerProgress.Instance.ReloadItem(itemID);
                // Sync local variable with PlayerProgress
                ammunitionCurrent = PlayerProgress.Instance.GetItemCurrent(itemID);
                Debug.Log($"[Weapon] Reload END: ammunitionCurrent={ammunitionCurrent}, total={PlayerProgress.Instance.GetItemTotal(itemID)}");
            }
        }
        
        // CONCEITO: Cache da PooledObject do prefabProjectile pra saber
        // se devemos usar o pool ou Instantiate. Preenchido no Awake.
        private bool useProjectilePool = false;

        /// <summary>
        /// Fired every time any weapon fires. Includes a reference to the Weapon that fired.
        /// Used by EasterEggTarget to track which weapon the player is using.
        /// </summary>
        public static event System.Action<Weapon> OnWeaponFired;

        public override void Fire(float spreadMultiplier = 1.0f) {
            // SEGURANÇA: Verificações defensivas para evitar NullReferenceException
            // quando Weapon é ativado mas ainda não foi inicializado completamente.
            if (muzzleBehaviour == null) {
                Debug.LogWarning($"[Weapon] Fire called but muzzleBehaviour is NULL!");
                return;
            }

            if (playerCamera == null) {
                Debug.LogWarning($"[Weapon] Fire called but playerCamera is NULL!");
                return;
            }

            if (animator == null) {
                Debug.LogWarning($"[Weapon] Fire called but animator is NULL!");
                return;
            }

            if (magazineBehaviour == null) {
                Debug.LogWarning($"[Weapon] Fire called but magazineBehaviour is NULL!");
                return;
            }

            Transform muzzleSocket = muzzleBehaviour.GetSocket();
            if (muzzleSocket == null) {
                Debug.LogWarning($"[Weapon] Fire called but muzzleSocket is NULL!");
                return;
            }

            const string stateName = "Fire";
            animator.Play(stateName, 0, 0.0f);
            
            // NEW: Decrement ammo from current magazine
            // This directly affects what is displayed on the HUD
            ammunitionCurrent = Mathf.Max(0, ammunitionCurrent - 1);
            
            // SYNC: Update PlayerProgress to match local change
            if (PlayerProgress.Instance != null) {
                int ppBefore = PlayerProgress.Instance.GetItemCurrent(itemID);
                PlayerProgress.Instance.SetItemCurrent(itemID, ammunitionCurrent);
                if (ammunitionCurrent == 0 && ppBefore > 0) {
                    Debug.Log($"[Weapon] Fire: magazine EMPTY now. PP_current before={ppBefore}, total={PlayerProgress.Instance.GetItemTotal(itemID)}");
                }
            }

            muzzleBehaviour.Effect();

            Quaternion rotation = Quaternion.LookRotation(
                playerCamera.position + playerCamera.forward * 1000.0f - muzzleSocket.position);

            if (Physics.Raycast(new Ray(playerCamera.position, playerCamera.forward),
                out RaycastHit hit, maximumDistance, mask))
                rotation = Quaternion.LookRotation(hit.point - muzzleSocket.position);

            // CONCEITO: Usa o pool de objetos em vez de Instantiate.
            // O pool reutiliza projéteis que já foram "destruídos" (desativados).
            // Isso reduz drasticamente o garbage collection (GC).
            // Se o pool estiver vazio, ele cria um novo automaticamente.
            GameObject projectile;
            if (useProjectilePool) {
                projectile = GameObjectPool.Get(prefabProjectile, muzzleSocket.position, rotation);
            } else {
                projectile = Instantiate(prefabProjectile, muzzleSocket.position, rotation);
            }

            // Set projectile damage from weapon's current stats
            Projectile projectileScript = projectile.GetComponent<Projectile>();
            if (projectileScript != null) {
                projectileScript.damage = currentDamage;
            }

            Rigidbody projectileRb = projectile.GetComponent<Rigidbody>();
            projectileRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            projectileRb.linearVelocity = projectile.transform.forward * projectileImpulse;

            // Notify systems that care about weapon fire (e.g. Easter egg)
            OnWeaponFired?.Invoke(this);
        }

        public override void FillAmmunition(int amount) {
            // FIXED: When amount == 0 (called from reload animation event), sync from PlayerProgress
            // instead of bypassing the ammo system by filling to max unconditionally.
            // This ensures the reserve ammo system is respected and UI stays in sync.
            if (amount == 0) {
                // Sync local ammunitionCurrent from PlayerProgress (already updated by ReloadItem)
                // This respects the actual ammo transferred from reserve to magazine.
                if (PlayerProgress.Instance != null) {
                    ammunitionCurrent = PlayerProgress.Instance.GetItemCurrent(itemID);
                } else {
                    // Fallback if no PlayerProgress: keep current value unchanged
                }
            } else {
                // For explicit amount fills (e.g. from power-ups), clamp to magazine capacity
                ammunitionCurrent = Mathf.Clamp(ammunitionCurrent + amount,
                    0, GetAmmunitionTotal());
            }
            
            // SYNC: Always sync back to PlayerProgress after any fill operation
            // This ensures the HUD (which reads from PlayerProgress) shows the correct value
            if (PlayerProgress.Instance != null) {
                PlayerProgress.Instance.SetItemCurrent(itemID, ammunitionCurrent);
            }
        }

        /// <summary>
        /// Ejects a casing from the weapon's ejection port.
        /// CONCEITO: Em builds WebGL, casings são desativados completamente
        /// porque cada Instantiate causa GC pressure.
        /// Em outras plataformas, usa pool pra reutilizar casings.
        /// </summary>
        public override void EjectCasing() {
#if UNITY_WEBGL
            // CONCEITO: WebGL não precisa de casings — economia de GPU e GC
            return;
#else
            if (prefabCasing != null && socketEjection != null)
            {
                // CONCEITO: Pool de casings — reusa em vez de Instantiate/Destroy
                GameObject casing = GameObjectPool.Get(prefabCasing, socketEjection.position, socketEjection.rotation);
                // CONCEITO: Devolve a casing ao pool após 5 segundos
                StartCoroutine(ReturnCasingAfterDelay(casing));
            }
#endif
        }

        private static IEnumerator ReturnCasingAfterDelay(GameObject casing) {
            yield return new WaitForSeconds(5f);
            if (casing != null && casing.TryGetComponent(out PooledObject pooled))
                pooled.ReturnToPool();
        }

        #endregion
    }
}