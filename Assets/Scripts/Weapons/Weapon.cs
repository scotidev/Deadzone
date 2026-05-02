// Copyright 2021, Infima Games. All Rights Reserved.

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

        [SerializeField] private bool automatic;
        [SerializeField] private float projectileImpulse = 400.0f;
        [SerializeField] private int roundsPerMinutes = 200;

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

        private MagazineBehaviour magazineBehaviour;
        private MuzzleBehaviour muzzleBehaviour;

        #endregion

        #region UNITY

        protected override void Awake() {
            animator = GetComponent<Animator>();
            if (animator == null) {
                Debug.LogWarning($"[Weapon.Awake] No Animator found on {gameObject.name}! Animation will not work.");
            }
            
            attachmentManager = GetComponent<WeaponAttachmentManagerBehaviour>();
            if (attachmentManager == null) {
                Debug.LogWarning($"[Weapon.Awake] No WeaponAttachmentManager found on {gameObject.name}! Attachments will not work.");
            }

            gameModeService = ServiceLocator.Current.Get<IGameModeService>();
            characterBehaviour = gameModeService.GetPlayerCharacter();
            playerCamera = characterBehaviour.GetCameraWorld().transform;
        }
        protected override void Start() {
            if (attachmentManager != null) {
                magazineBehaviour = attachmentManager.GetEquippedMagazine();
                muzzleBehaviour = attachmentManager.GetEquippedMuzzle();
            }

            if (magazineBehaviour != null) {
                ammunitionCurrent = magazineBehaviour.GetAmmunitionTotal();
            } else {
                Debug.LogWarning($"[Weapon.Start] No magazineBehaviour on {gameObject.name}! Ammunition will be 0.");
                ammunitionCurrent = 0;
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

        public override int GetAmmunitionTotal() => magazineBehaviour != null ? magazineBehaviour.GetAmmunitionTotal() : 0;

        public override bool IsAutomatic() => automatic;
        public override float GetRateOfFire() => roundsPerMinutes;

        public override bool IsFull() => magazineBehaviour != null && ammunitionCurrent == magazineBehaviour.GetAmmunitionTotal();
        public override bool HasAmmunition() => ammunitionCurrent > 0;

        public override RuntimeAnimatorController GetAnimatorController() => controller;
        public override WeaponAttachmentManagerBehaviour GetAttachmentManager() => attachmentManager;

        #endregion

        #region METHODS

        public override void Reload() {
            if (animator == null || !animator.enabled)
                return;
                
            animator.Play(HasAmmunition() ? "Reload" : "Reload Empty", 0, 0.0f);
        }
        public override void Fire(float spreadMultiplier = 1.0f) {
            if (animator == null || !animator.enabled || !gameObject.activeInHierarchy) {
                Debug.LogWarning($"[Weapon.Fire] Cannot fire - animator is null, disabled, or game object is inactive on {gameObject.name}");
                return;
            }

            if (muzzleBehaviour == null)
                return;

            if (playerCamera == null)
                return;

            Transform muzzleSocket = muzzleBehaviour.GetSocket();
            if (muzzleSocket == null) {
                Debug.LogWarning($"[Weapon.Fire] No muzzle socket on {gameObject.name}");
                return;
            }

            const string stateName = "Fire";
            animator.Play(stateName, 0, 0.0f);
            ammunitionCurrent = Mathf.Clamp(ammunitionCurrent - 1, 0, magazineBehaviour != null ? magazineBehaviour.GetAmmunitionTotal() : 0);

            muzzleBehaviour.Effect();

            Quaternion rotation = Quaternion.LookRotation(
                playerCamera.position + playerCamera.forward * 1000.0f - muzzleSocket.position);

            if (Physics.Raycast(new Ray(playerCamera.position, playerCamera.forward),
                out RaycastHit hit, maximumDistance, mask))
                rotation = Quaternion.LookRotation(hit.point - muzzleSocket.position);

            GameObject projectile = Instantiate(prefabProjectile, muzzleSocket.position, rotation);

            Rigidbody projectileRb = projectile.GetComponent<Rigidbody>();
            projectileRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            projectileRb.linearVelocity = projectile.transform.forward * projectileImpulse;
        }

        public override void FillAmmunition(int amount) {
            int maxAmmo = magazineBehaviour != null ? magazineBehaviour.GetAmmunitionTotal() : 0;
            ammunitionCurrent = amount != 0 ? Mathf.Clamp(ammunitionCurrent + amount, 0, maxAmmo) : maxAmmo;
        }

        public override void EjectCasing() {
            if (prefabCasing != null && socketEjection != null)
                Instantiate(prefabCasing, socketEjection.position, socketEjection.rotation);
        }

        #endregion
    }
}