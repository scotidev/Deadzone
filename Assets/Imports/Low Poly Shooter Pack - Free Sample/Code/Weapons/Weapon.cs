// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Weapon. This class handles most of the things that weapons need.
    /// </summary>
    /// <summary>
    /// Script que gerencia o comportamento individual de cada arma.
    /// </summary>
    public class Weapon : WeaponBehaviour
    {
        #region FIELDS SERIALIZED

        [Header("Configurações de Tiro")]

        [Tooltip("A arma é automática? Se sim, segurar o botão de tiro disparará continuamente.")]
        [SerializeField]
        private bool automatic;

        [Tooltip("Velocidade/Impulso do projétil.")]
        [SerializeField]
        private float projectileImpulse = 400.0f;

        [Tooltip("Cadência de tiro (tiros por minuto).")]
        [SerializeField]
        private int roundsPerMinutes = 200;

        [Tooltip("Camadas (Layers) que o tiro pode atingir.")]
        [SerializeField]
        private LayerMask mask;

        [Tooltip("Distância máxima para precisão total.")]
        [SerializeField]
        private float maximumDistance = 500.0f;

        [Header("Animação")]

        [Tooltip("Ponto de onde saem as cápsulas vazias.")]
        [SerializeField]
        private Transform socketEjection;

        [Header("Recursos (Prefabs)")]

        [Tooltip("Prefab da cápsula vazia.")]
        [SerializeField]
        private GameObject prefabCasing;

        [Tooltip("Prefab do projétil (bala) que será disparado.")]
        [SerializeField]
        private GameObject prefabProjectile;

        [Tooltip("The AnimatorController a player character needs to use while wielding this weapon.")]
        [SerializeField]
        public RuntimeAnimatorController controller;

        [Tooltip("Weapon Body Texture.")]
        [SerializeField]
        private Sprite spriteBody;

        [Header("Audio Clips Holster")]

        [Tooltip("Holster Audio Clip.")]
        [SerializeField]
        private AudioClip audioClipHolster;

        [Tooltip("Unholster Audio Clip.")]
        [SerializeField]
        private AudioClip audioClipUnholster;

        [Header("Audio Clips Reloads")]

        [Tooltip("Reload Audio Clip.")]
        [SerializeField]
        private AudioClip audioClipReload;

        [Tooltip("Reload Empty Audio Clip.")]
        [SerializeField]
        private AudioClip audioClipReloadEmpty;

        [Header("Audio Clips Other")]

        [Tooltip("AudioClip played when this weapon is fired without any ammunition.")]
        [SerializeField]
        private AudioClip audioClipFireEmpty;

        #endregion

        #region FIELDS

        /// <summary>
        /// Weapon Animator.
        /// </summary>
        private Animator animator;
        /// <summary>
        /// Attachment Manager.
        /// </summary>
        private WeaponAttachmentManagerBehaviour attachmentManager;

        /// <summary>
        /// Amount of ammunition left.
        /// </summary>
        private int ammunitionCurrent;

        #region Attachment Behaviours

        /// <summary>
        /// Equipped Magazine Reference.
        /// </summary>
        private MagazineBehaviour magazineBehaviour;
        /// <summary>
        /// Equipped Muzzle Reference.
        /// </summary>
        private MuzzleBehaviour muzzleBehaviour;

        #endregion

        /// <summary>
        /// The GameModeService used in this game!
        /// </summary>
        private IGameModeService gameModeService;
        /// <summary>
        /// The main player character behaviour component.
        /// </summary>
        private CharacterBehaviour characterBehaviour;

        /// <summary>
        /// The player character's camera.
        /// </summary>
        private Transform playerCamera;

        #endregion

        #region UNITY

        protected override void Awake()
        {
            // Cacheia o Animator da arma.
            animator = GetComponent<Animator>();
            // Cacheia o gerenciador de acessórios (miras, silenciadores, etc).
            attachmentManager = GetComponent<WeaponAttachmentManagerBehaviour>();

            // Busca referências globais necessárias para o funcionamento da arma.
            gameModeService = ServiceLocator.Current.Get<IGameModeService>();
            characterBehaviour = gameModeService.GetPlayerCharacter();
            // Pega a câmera do jogador para saber para onde ele está olhando ao atirar.
            playerCamera = characterBehaviour.GetCameraWorld().transform;
        }
        protected override void Start()
        {
            #region Cache Attachment References

            // Pega as referências do pente (magazine) e do bocal (muzzle) equipados.
            magazineBehaviour = attachmentManager.GetEquippedMagazine();
            muzzleBehaviour = attachmentManager.GetEquippedMuzzle();

            #endregion

            // Começa com a munição máxima permitida pelo pente.
            ammunitionCurrent = magazineBehaviour.GetAmmunitionTotal();
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

        public override AudioClip GetAudioClipFire() => muzzleBehaviour.GetAudioClipFire();

        public override int GetAmmunitionCurrent() => ammunitionCurrent;

        public override int GetAmmunitionTotal() => magazineBehaviour.GetAmmunitionTotal();

        public override bool IsAutomatic() => automatic;
        public override float GetRateOfFire() => roundsPerMinutes;

        public override bool IsFull() => ammunitionCurrent == magazineBehaviour.GetAmmunitionTotal();
        public override bool HasAmmunition() => ammunitionCurrent > 0;

        public override RuntimeAnimatorController GetAnimatorController() => controller;
        public override WeaponAttachmentManagerBehaviour GetAttachmentManager() => attachmentManager;

        #endregion

        #region METHODS

        public override void Reload()
        {
            //Play Reload Animation.
            animator.Play(HasAmmunition() ? "Reload" : "Reload Empty", 0, 0.0f);
        }
        public override void Fire(float spreadMultiplier = 1.0f)
        {
            // Precisa de um bocal (muzzle) configurado para sair o tiro.
            if (muzzleBehaviour == null)
                return;

            // Precisa da câmera para calcular a direção.
            if (playerCamera == null)
                return;

            // Ponto de origem do tiro (ponta da arma).
            Transform muzzleSocket = muzzleBehaviour.GetSocket();

            // Toca a animação de tiro da própria arma.
            const string stateName = "Fire";
            animator.Play(stateName, 0, 0.0f);

            // Reduz 1 bala do pente atual.
            ammunitionCurrent = Mathf.Clamp(ammunitionCurrent - 1, 0, magazineBehaviour.GetAmmunitionTotal());

            // Toca efeitos visuais (fogo, fumaça) e sonoros do bocal.
            muzzleBehaviour.Effect();

            // Calcula a rotação inicial baseada no centro da tela (para onde a câmera olha).
            Quaternion rotation = Quaternion.LookRotation(playerCamera.forward * 1000.0f - muzzleSocket.position);

            // Raycast: se houver um objeto na frente, ajusta a rotação para a bala ir exatamente no ponto central.
            if (Physics.Raycast(new Ray(playerCamera.position, playerCamera.forward),
                out RaycastHit hit, maximumDistance, mask))
                rotation = Quaternion.LookRotation(hit.point - muzzleSocket.position);

            // Cria o objeto do projétil no mundo.
            GameObject projectile = Instantiate(prefabProjectile, muzzleSocket.position, rotation);
            // Dá velocidade física ao projétil.
            projectile.GetComponent<Rigidbody>().linearVelocity = projectile.transform.forward * projectileImpulse;
        }

        public override void FillAmmunition(int amount)
        {
            //Update the value by a certain amount.
            ammunitionCurrent = amount != 0 ? Mathf.Clamp(ammunitionCurrent + amount,
                0, GetAmmunitionTotal()) : magazineBehaviour.GetAmmunitionTotal();
        }

        public override void EjectCasing()
        {
            //Spawn casing prefab at spawn point.
            if (prefabCasing != null && socketEjection != null)
                Instantiate(prefabCasing, socketEjection.position, socketEjection.rotation);
        }

        #endregion
    }
}