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

        // Awake é chamado quando o script da arma nasce.
        protected override void Awake()
        {
            // Pega o Animator da arma. Cada arma tem o seu para animações de recarga próprias.
            animator = GetComponent<Animator>();
            // Pega o gerenciador de acessórios. Ele sabe se a arma tem mira telescópica, etc.
            attachmentManager = GetComponent<WeaponAttachmentManagerBehaviour>();

            // Busca os serviços globais (como o Service Locator que explicamos antes).
            gameModeService = ServiceLocator.Current.Get<IGameModeService>();
            // Guarda quem é o jogador que está segurando esta arma.
            characterBehaviour = gameModeService.GetPlayerCharacter();
            // Pega a Câmera do mundo. Isso é vital para saber exatamente para onde o jogador está mirando.
            playerCamera = characterBehaviour.GetCameraWorld().transform;
        }

        // Start roda logo depois do Awake.
        protected override void Start()
        {
            #region Cache Attachment References

            // Pergunta ao attachmentManager: "Qual pente (magazine) e qual bico (muzzle) eu estou usando?"
            magazineBehaviour = attachmentManager.GetEquippedMagazine();
            muzzleBehaviour = attachmentManager.GetEquippedMuzzle();

            #endregion

            // No começo do jogo, enche o pente da arma com a capacidade total dele.
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
        /// <summary>
        /// Faz a arma disparar.
        /// </summary>
        public override void Fire(float spreadMultiplier = 1.0f)
        {
            // Se o script não achar o bico da arma (de onde sai o fogo), ele para aqui para não dar erro.
            if (muzzleBehaviour == null)
                return;

            // Se não achar a câmera do jogador, para aqui (não saberia para onde atirar).
            if (playerCamera == null)
                return;

            // muzzleSocket é o ponto exato no modelo 3D onde a bala aparece.
            Transform muzzleSocket = muzzleBehaviour.GetSocket();

            // Toca a animação da própria arma (ex: o ferrolho se movendo).
            const string stateName = "Fire";
            animator.Play(stateName, 0, 0.0f);

            // Tira uma bala do pente. O 'Mathf.Clamp' garante que o número nunca seja menor que zero.
            ammunitionCurrent = Mathf.Clamp(ammunitionCurrent - 1, 0, magazineBehaviour.GetAmmunitionTotal());

            // Manda o bico da arma soltar o clarão (muzzle flash) e o som do tiro.
            muzzleBehaviour.Effect();

            // Calcula uma direção inicial "chutada" para a bala, indo para a frente da câmera.
            Quaternion rotation = Quaternion.LookRotation(playerCamera.forward * 1000.0f - muzzleSocket.position);

            // RAYCAST: Isso é um "laser invisível" que sai do centro da câmera.
            // Se esse laser bater em algo (parede, inimigo), a arma ajusta o cano para a bala ir EXATAMENTE onde o laser bateu.
            // Isso garante que o tiro saia do cano da arma mas acerte onde a cruz (crosshair) está apontando.
            if (Physics.Raycast(new Ray(playerCamera.position, playerCamera.forward),
                out RaycastHit hit, maximumDistance, mask))
                rotation = Quaternion.LookRotation(hit.point - muzzleSocket.position);

            // 'Instantiate' cria o objeto da bala (prefabProjectile) no jogo.
            GameObject projectile = Instantiate(prefabProjectile, muzzleSocket.position, rotation);
            // Pega o Rigidbody da bala e empurra ele para frente com muita força (projectileImpulse).
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