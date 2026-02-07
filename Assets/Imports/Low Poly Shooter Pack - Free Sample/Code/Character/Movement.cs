// Copyright 2021, Infima Games. All Rights Reserved.

using System.Linq;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class Movement : MovementBehaviour
    {
        #region FIELDS SERIALIZED

        [Header("Sons")]

        [Tooltip("Som tocado quando o jogador está andando.")]
        [SerializeField]
        private AudioClip audioClipWalking;

        [Tooltip("Som tocado quando o jogador está correndo.")]
        [SerializeField]
        private AudioClip audioClipRunning;

        [Header("Velocidades")]

        [Tooltip("Velocidade de caminhada.")]
        [SerializeField]
        private float speedWalking = 5.0f;

        [Tooltip("Velocidade de corrida."), SerializeField]
        private float speedRunning = 9.0f;

        #endregion

        #region PROPERTIES

        //Velocity.
        private Vector3 Velocity
        {
            //Getter.
            get => rigidBody.linearVelocity;
            //Setter.
            set => rigidBody.linearVelocity = value;
        }

        #endregion

        #region FIELDS

        /// <summary>
        /// Attached Rigidbody.
        /// </summary>
        private Rigidbody rigidBody;
        /// <summary>
        /// Attached CapsuleCollider.
        /// </summary>
        private CapsuleCollider capsule;
        /// <summary>
        /// Attached AudioSource.
        /// </summary>
        private AudioSource audioSource;

        /// <summary>
        /// True if the character is currently grounded.
        /// </summary>
        private bool grounded;

        /// <summary>
        /// Player Character.
        /// </summary>
        private CharacterBehaviour playerCharacter;
        /// <summary>
        /// The player character's equipped weapon.
        /// </summary>
        private WeaponBehaviour equippedWeapon;

        /// <summary>
        /// Array of RaycastHits used for ground checking.
        /// </summary>
        private readonly RaycastHit[] groundHits = new RaycastHit[8];

        #endregion

        #region UNITY FUNCTIONS

        /// <summary>
        /// Awake é chamado quando o script é carregado.
        /// </summary>
        protected override void Awake()
        {
            // Busca a referência do personagem principal através do Service Locator.
            playerCharacter = ServiceLocator.Current.Get<IGameModeService>().GetPlayerCharacter();
        }

        /// Inicializa o controlador de movimento no Start.
        protected override void Start()
        {
            // Configura o Rigidbody (física).
            rigidBody = GetComponent<Rigidbody>();
            // Trava a rotação para o personagem não "tombar" com a física.
            rigidBody.constraints = RigidbodyConstraints.FreezeRotation;

            // Cacheia o CapsuleCollider.
            capsule = GetComponent<CapsuleCollider>();

            // Configuração básica do AudioSource para os sons de passos.
            audioSource = GetComponent<AudioSource>();
            audioSource.clip = audioClipWalking;
            audioSource.loop = true;
        }

        /// Verifica se o personagem está tocando o chão usando colisões.
        private void OnCollisionStay()
        {
            // Obtém os limites do colisor do personagem.
            Bounds bounds = capsule.bounds;
            Vector3 extents = bounds.extents;
            float radius = extents.x - 0.01f;

            // Faz um "SphereCast" (uma esfera invisível disparada para baixo) para checar o chão.
            Physics.SphereCastNonAlloc(bounds.center, radius, Vector3.down,
                groundHits, extents.y - radius * 0.5f, ~0, QueryTriggerInteraction.Ignore);

            // Se a esfera atingiu algo que não seja o próprio jogador, estamos no chão.
            if (!groundHits.Any(hit => hit.collider != null && hit.collider != capsule))
                return;

            // Limpa os hits para o próximo frame.
            for (var i = 0; i < groundHits.Length; i++)
                groundHits[i] = new RaycastHit();

            // Marca como aterrado.
            grounded = true;
        }

        protected override void FixedUpdate()
        {
            // Move o personagem. FixedUpdate é melhor para cálculos de física (Rigidbody).
            MoveCharacter();

            // Reseta o estado de grounded para ser verificado novamente no próximo frame de física.
            grounded = false;
        }

        /// Moves the camera to the character, processes jumping and plays sounds every frame.
        protected override void Update()
        {
            //Get the equipped weapon!
            equippedWeapon = playerCharacter.GetInventory().GetEquipped();

            //Play Sounds!
            PlayFootstepSounds();
        }

        #endregion

        #region METHODS

        private void MoveCharacter()
        {
            #region Calculate Movement Velocity

            // Obtém o input (WASD) do script do Personagem.
            Vector2 frameInput = playerCharacter.GetInputMovement();
            // Transforma o input 2D em um vetor 3D de direção.
            var movement = new Vector3(frameInput.x, 0.0f, frameInput.y);

            // Define a velocidade baseada se o jogador está correndo ou andando.
            if (playerCharacter.IsRunning())
                movement *= speedRunning;
            else
            {
                movement *= speedWalking;
            }

            // Converte a direção do movimento de "local" para "mundo" (faz o WASD seguir a rotação do jogador).
            movement = transform.TransformDirection(movement);

            #endregion

            // Aplica a velocidade calculada ao Rigidbody (mantendo o Y como zero para não afetar a gravidade aqui).
            Velocity = new Vector3(movement.x, 0.0f, movement.z);
        }

        /// <summary>
        /// Plays Footstep Sounds. This code is slightly old, so may not be great, but it functions alright-y!
        /// </summary>
        private void PlayFootstepSounds()
        {
            //Check if we're moving on the ground. We don't need footsteps in the air.
            if (grounded && rigidBody.linearVelocity.sqrMagnitude > 0.1f)
            {
                //Select the correct audio clip to play.
                audioSource.clip = playerCharacter.IsRunning() ? audioClipRunning : audioClipWalking;
                //Play it!
                if (!audioSource.isPlaying)
                    audioSource.Play();
            }
            //Pause it if we're doing something like flying, or not moving!
            else if (audioSource.isPlaying)
                audioSource.Pause();
        }

        #endregion
    }
}