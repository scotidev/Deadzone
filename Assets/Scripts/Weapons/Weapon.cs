using System.Collections;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Weapon. Handles firing, reloading, ammo management, stat scaling from WeaponDataSO,
    /// and attachment integration (magazine, muzzle, scope). Central hub for all weapon gameplay logic.
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
        private bool hasInitializedAmmo;
        private MagazineBehaviour magazineBehaviour;
        private MuzzleBehaviour muzzleBehaviour;
        private float currentDamage;
        private float currentFireRate;
        private bool isAutomatic;
        private IAudioManagerService audioService;
        private bool useProjectilePool;

        #endregion

        #region EVENTS

        /// <summary>
        /// Fired every time any weapon fires. Includes a reference to the Weapon that fired.
        /// Used by EasterEggTarget to track which weapon the player is using.
        /// </summary>
        public static event System.Action<Weapon> OnWeaponFired;

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

            UpgradeManager.OnItemUpgraded += HandleItemUpgraded;

            if (prefabProjectile != null) {
                GameObjectPool.Prewarm(prefabProjectile, 10);
                useProjectilePool = true;
            }
        }

        private void OnDestroy() {
            UpgradeManager.OnItemUpgraded -= HandleItemUpgraded;
        }
        
        protected override void Start() {
            RefreshStats();
            InitializeWeapon();
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Refreshes the weapon's stats from WeaponDataSO based on the current upgrade level.
        /// </summary>
        public void RefreshStats() {
            if (weaponData == null) {
                currentDamage = 1f;
                currentFireRate = 1f;
                isAutomatic = false;
                return;
            }

            int level = PlayerProgress.Instance != null ? PlayerProgress.Instance.GetItemLevel(itemID) : 1;
            
            currentDamage = weaponData.GetDamageAtLevel(level);
            currentFireRate = weaponData.GetFireRateAtLevel(level);
            isAutomatic = weaponData.isAutomatic;

            if (currentFireRate <= 0) {
                currentFireRate = 1;
                Debug.LogWarning($"[Weapon] {itemID} has 0 fire rate in SO! Using 1 to avoid crash.");
            }
        }

        /// <summary>
        /// Handles upgrade events for this weapon. Refreshes stats and syncs ammo from PlayerProgress.
        /// </summary>
        private void HandleItemUpgraded(string upgradedItemID, ItemDataSO itemData) {
            if (upgradedItemID == itemID) {
                RefreshStats();
                
                if (PlayerProgress.Instance != null) {
                    ammunitionCurrent = PlayerProgress.Instance.GetItemCurrent(itemID);
                }
            }
        }

        /// <summary>
        /// Forces full weapon initialization on selection. Ensures components and ammo are ready.
        /// </summary>
        public void ForceInitialize() {
            RefreshStats();

            if (hasInitializedAmmo && magazineBehaviour != null && muzzleBehaviour != null) {
                return;
            }
            
            InitializeWeapon();
        }

        /// <summary>
        /// Initializes weapon components (magazine, muzzle) and syncs ammo from PlayerProgress.
        /// </summary>
        private void InitializeWeapon() {
            if (attachmentManager == null) {
                Debug.LogWarning($"[Weapon] InitializeWeapon: attachmentManager is NULL!");
                return;
            }

            magazineBehaviour = attachmentManager.GetEquippedMagazine();
            muzzleBehaviour = attachmentManager.GetEquippedMuzzle();

            if (magazineBehaviour != null) {
                if (PlayerProgress.Instance != null) {
                    int maxCurrent = magazineBehaviour.GetAmmunitionTotal();
                    
                    if (!PlayerProgress.Instance.IsAmmoInitialized(itemID)) {
                        PlayerProgress.Instance.SetItemCurrent(itemID, maxCurrent);
                        
                        int currentTotal = PlayerProgress.Instance.GetItemTotal(itemID);
                        if (currentTotal <= 0) {
                            PlayerProgress.Instance.SetItemTotal(itemID, maxCurrent);
                        }
                        PlayerProgress.Instance.MarkAmmoInitialized(itemID);
                        ammunitionCurrent = maxCurrent;
                    } else {
                        ammunitionCurrent = PlayerProgress.Instance.GetItemCurrent(itemID);
                    }
                    
                    hasInitializedAmmo = true;
                } else {
                    ammunitionCurrent = magazineBehaviour.GetAmmunitionTotal();
                }
            } else {
                Debug.LogWarning($"[Weapon] InitializeWeapon: magazineBehaviour is NULL after attachment lookup!");
            }

            if (muzzleBehaviour == null) {
                Debug.LogWarning($"[Weapon] InitializeWeapon: muzzleBehaviour is NULL after attachment lookup!");
            }
        }

        /// <summary>
        /// Reloads the weapon by transferring ammo from reserve to magazine via PlayerProgress.
        /// </summary>
        public override void Reload() {
            if (IsFull()) {
                return;
            }

            if (PlayerProgress.Instance != null) {
                int total = PlayerProgress.Instance.GetItemTotal(itemID);
                if (total <= 0) {
                    if (audioClipFireEmpty != null) {
                        audioService?.PlaySFX3D(audioClipFireEmpty, transform.position, 1f);
                    }
                    Debug.Log($"[Weapon] Cannot reload {itemID} - no reserve ammo available.");
                    return;
                }
            }

            animator.Play(HasAmmunition() ? "Reload" : "Reload Empty", 0, 0.0f);

            if (PlayerProgress.Instance != null) {
                PlayerProgress.Instance.ReloadItem(itemID);
                ammunitionCurrent = PlayerProgress.Instance.GetItemCurrent(itemID);
            }
        }

        /// <summary>
        /// Fires the weapon. Spawns a projectile, applies damage, and handles ammo decrement.
        /// Includes safety checks for null components before firing.
        /// </summary>
        public override void Fire(float spreadMultiplier = 1.0f) {
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
            
            ammunitionCurrent = Mathf.Max(0, ammunitionCurrent - 1);
            
            if (PlayerProgress.Instance != null) {
                PlayerProgress.Instance.SetItemCurrent(itemID, ammunitionCurrent);
            }

            muzzleBehaviour.Effect();

            Quaternion rotation = Quaternion.LookRotation(
                playerCamera.position + playerCamera.forward * 1000.0f - muzzleSocket.position);

            if (Physics.Raycast(new Ray(playerCamera.position, playerCamera.forward),
                out RaycastHit hit, maximumDistance, mask))
                rotation = Quaternion.LookRotation(hit.point - muzzleSocket.position);

            GameObject projectile;
            if (useProjectilePool) {
                projectile = GameObjectPool.Get(prefabProjectile, muzzleSocket.position, rotation);
            } else {
                projectile = Instantiate(prefabProjectile, muzzleSocket.position, rotation);
            }

            Projectile projectileScript = projectile.GetComponent<Projectile>();
            if (projectileScript != null) {
                projectileScript.damage = currentDamage;
            }

            Rigidbody projectileRb = projectile.GetComponent<Rigidbody>();
            projectileRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            projectileRb.linearVelocity = projectile.transform.forward * projectileImpulse;

            OnWeaponFired?.Invoke(this);
        }

        /// <summary>
        /// Fills the weapon's ammunition by a given amount. Syncs back to PlayerProgress.
        /// </summary>
        public override void FillAmmunition(int amount) {
            if (amount == 0) {
                if (PlayerProgress.Instance != null) {
                    ammunitionCurrent = PlayerProgress.Instance.GetItemCurrent(itemID);
                }
            } else {
                ammunitionCurrent = Mathf.Clamp(ammunitionCurrent + amount,
                    0, GetAmmunitionTotal());
            }
            
            if (PlayerProgress.Instance != null) {
                PlayerProgress.Instance.SetItemCurrent(itemID, ammunitionCurrent);
            }
        }

        /// <summary>
        /// Ejects a casing from the weapon's ejection port using the object pool.
        /// Casings are disabled entirely on WebGL builds to avoid GC pressure.
        /// </summary>
        public override void EjectCasing() {
#if UNITY_WEBGL
            return;
#else
            if (prefabCasing != null && socketEjection != null)
            {
                GameObject casing = GameObjectPool.Get(prefabCasing, socketEjection.position, socketEjection.rotation);
                StartCoroutine(ReturnCasingAfterDelay(casing));
            }
#endif
        }

        /// <summary>
        /// Returns a casing GameObject to the object pool after a delay.
        /// </summary>
        private static IEnumerator ReturnCasingAfterDelay(GameObject casing) {
            yield return new WaitForSeconds(5f);
            if (casing != null && casing.TryGetComponent(out PooledObject pooled))
                pooled.ReturnToPool();
        }

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
        /// Returns total ammunition capacity of the equipped magazine. Returns 0 if magazine is null.
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
        /// Checks if the magazine is completely full.
        /// </summary>
        public override bool IsFull() {
            if (magazineBehaviour == null) return false;
            return ammunitionCurrent == magazineBehaviour.GetAmmunitionTotal();
        }
        
        public override bool HasAmmunition() => ammunitionCurrent > 0;

        public override RuntimeAnimatorController GetAnimatorController() => controller;
        public override WeaponAttachmentManagerBehaviour GetAttachmentManager() => attachmentManager;

        #endregion

        #endregion
    }
}
