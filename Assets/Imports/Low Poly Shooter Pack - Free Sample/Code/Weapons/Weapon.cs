// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Weapon. This class handles most of the things that weapons need.
    /// </summary>
    public class Weapon : WeaponBehaviour
    {
        #region FIELDS SERIALIZED
        
        [Header("Firing")]

        [Tooltip("Is this weapon automatic? If yes, then holding down the firing button will continuously fire.")]
        [SerializeField] 
        private bool automatic;
        
        [Tooltip("How fast the projectiles are.")]
        [SerializeField]
        private float projectileImpulse = 400.0f;

        [Tooltip("Amount of shots this weapon can shoot in a minute. It determines how fast the weapon shoots.")]
        [SerializeField] 
        private int roundsPerMinutes = 200;

        [Tooltip("Mask of things recognized when firing.")]
        [SerializeField]
        private LayerMask mask;

        [Tooltip("Maximum distance at which this weapon can fire accurately. Shots beyond this distance will not use linetracing for accuracy.")]
        [SerializeField]
        private float maximumDistance = 500.0f;

        [Header("Animation")]

        [Tooltip("Transform that represents the weapon's ejection port, meaning the part of the weapon that casings shoot from.")]
        [SerializeField]
        private Transform socketEjection;

        [Header("Resources")]

        [Tooltip("Casing Prefab.")]
        [SerializeField]
        private GameObject prefabCasing;
        
        [Tooltip("Projectile Prefab. This is the prefab spawned when the weapon shoots.")]
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
            //Get Animator.
            animator = GetComponent<Animator>();
            //Get Attachment Manager.
            attachmentManager = GetComponent<WeaponAttachmentManagerBehaviour>();

            //Cache the game mode service. We only need this right here, but we'll cache it in case we ever need it again.
            gameModeService = ServiceLocator.Current.Get<IGameModeService>();
            //Cache the player character.
            characterBehaviour = gameModeService.GetPlayerCharacter();
            //Cache the world camera. We use this in line traces.
            playerCamera = characterBehaviour.GetCameraWorld().transform;
        }
        protected override void Start()
        {
            #region Cache Attachment References
            
            //Get Magazine.
            magazineBehaviour = attachmentManager.GetEquippedMagazine();
            //Get Muzzle.
            muzzleBehaviour = attachmentManager.GetEquippedMuzzle();

            #endregion

            //Max Out Ammo.
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
            //We need a muzzle in order to fire this weapon!
            if (muzzleBehaviour == null)
                return;
            
            //Make sure that we have a camera cached, otherwise we don't really have the ability to perform traces.
            if (playerCamera == null)
                return;

            //Get Muzzle Socket. This is the point we fire from.
            Transform muzzleSocket = muzzleBehaviour.GetSocket();
            
            //Play the firing animation.
            const string stateName = "Fire";
            animator.Play(stateName, 0, 0.0f);
            //Reduce ammunition! We just shot, so we need to get rid of one!
            ammunitionCurrent = Mathf.Clamp(ammunitionCurrent - 1, 0, magazineBehaviour.GetAmmunitionTotal());

            //Play all muzzle effects.
            muzzleBehaviour.Effect();
            
            // ==============================================================
            //  [CORRIGIDO] Cálculo do ponto-alvo do projétil (fallback)
            // ==============================================================
            //  AULA: QUAL É A DIFERENÇA ENTRE VETOR DE DIREÇÃO E POSIÇÃO?
            //
            //  No código original havia este erro:
            //      playerCamera.forward * 1000.0f - muzzleSocket.position
            //
            //  "playerCamera.forward" é um VETOR DE DIREÇÃO — ele diz "para
            //  onde a câmera está olhando", mas não diz "de onde". Multiplicar
            //  por 1000 apenas escala esse vetor, ele continua sendo uma direção,
            //  não uma posição real no mundo.
            //
            //  Sem a posição da câmera, o ponto-alvo era calculado como se a
            //  câmera estivesse na origem do mundo (0, 0, 0). Quanto mais longe
            //  o jogador estivesse da origem, maior o desvio da bala — por isso
            //  o tiro saía levemente torto em algumas posições da cena.
            //
            //  CORREÇÃO:
            //      playerCamera.position + playerCamera.forward * 1000.0f
            //
            //  Agora somamos a POSIÇÃO real da câmera ao vetor de direção.
            //  O resultado é um ponto concreto no mundo: "1000 unidades à frente
            //  de onde a câmera realmente está". Subtraindo a posição do cano
            //  (muzzleSocket), obtemos a direção exata que alinha o tiro com
            //  o centro da tela (crosshair).
            //
            //  ANALOGIA: é como a diferença entre dizer "vá para frente" (direção)
            //  e "vá para frente a partir da sua posição atual" (posição + direção).
            // [CORRIGIDO] Adicionado playerCamera.position para usar posição real da câmera.
            // Antes era apenas playerCamera.forward * 1000.0f (sem a posição), o que
            // causava desvio na bala dependendo de onde o jogador estava na cena.
            Quaternion rotation = Quaternion.LookRotation(
                playerCamera.position + playerCamera.forward * 1000.0f - muzzleSocket.position);

            //If there's something blocking, then we can aim directly at that thing, which will result in more accurate shooting.
            if (Physics.Raycast(new Ray(playerCamera.position, playerCamera.forward),
                out RaycastHit hit, maximumDistance, mask))
                rotation = Quaternion.LookRotation(hit.point - muzzleSocket.position);

            //Spawn projectile from the projectile spawn point.
            GameObject projectile = Instantiate(prefabProjectile, muzzleSocket.position, rotation);

            // ==============================================================
            //  [CORRIGIDO] Modo de detecção de colisão do projétil
            // ==============================================================
            //  AULA: O QUE É "BULLET TUNNELING" (TUNELAMENTO DE BALA)?
            //
            //  A Unity verifica colisões a cada frame físico (FixedUpdate).
            //  O padrão é 50 frames por segundo, ou seja, a cada 0,02 segundos.
            //
            //  O projétil se move a 400 unidades/segundo. Então a cada frame:
            //      400 × 0,02 = 8 METROS de deslocamento por frame
            //
            //  O barril tem ~1m de largura. Se o projétil pula 8m por frame,
            //  ele pode PASSAR DIRETO pelo barril sem a Unity perceber — porque
            //  ela só checa a posição atual, não o caminho percorrido.
            //  Esse fenômeno se chama "bullet tunneling" (tunelamento de bala).
            //
            //  AULA: QUAL É A DIFERENÇA ENTRE OS MODOS?
            //
            //  Discrete (padrão)   → checa apenas a posição atual a cada frame.
            //                        Rápido, mas perde colisões com objetos finos.
            //
            //  Continuous          → calcula o CAMINHO percorrido entre frames,
            //                        mas apenas contra objetos SEM Rigidbody (estáticos).
            //                        Parede, chão e teto são detectados — o barril, não.
            //
            //  ContinuousDynamic   → calcula o caminho contra estáticos E dinâmicos.
            //                        "Dinâmico" = objeto que TEM um Rigidbody.
            //                        O barril tem Rigidbody (para receber força da
            //                        explosão), então só este modo o detecta corretamente.
            //
            //  RESUMO: trocamos Continuous por ContinuousDynamic para garantir
            //  que o projétil sempre colida com o barril, independente da velocidade.
            Rigidbody projectileRb = projectile.GetComponent<Rigidbody>();
            // [CORRIGIDO] Era CollisionDetectionMode.Continuous — não detectava o barril
            // porque ele tem Rigidbody. ContinuousDynamic cobre objetos dinâmicos também.
            projectileRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            //Add velocity to the projectile.
            projectileRb.linearVelocity = projectile.transform.forward * projectileImpulse;
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
            if(prefabCasing != null && socketEjection != null)
                Instantiate(prefabCasing, socketEjection.position, socketEjection.rotation);
        }

        #endregion
    }
}