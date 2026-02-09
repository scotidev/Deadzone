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
        /// Awake é chamado quando o script nasce.
        /// </summary>
        protected override void Awake()
        {
            // O Kit usa um "ServiceLocator" (tipo uma lista telefônica de scripts importantes).
            // Aqui ele pede para encontrar quem é o "Jogador Principal" e guarda essa referência.
            playerCharacter = ServiceLocator.Current.Get<IGameModeService>().GetPlayerCharacter();
        }

        /// Inicializa o controlador de movimento logo após o Awake.
        protected override void Start()
        {
            // O Rigidbody é o componente da Unity que faz o objeto ter peso e colidir com as coisas.
            rigidBody = GetComponent<Rigidbody>();
            // Avisa a Unity que a física NÃO deve girar o boneco (senão ele cairia de lado como um pino de boliche).
            rigidBody.constraints = RigidbodyConstraints.FreezeRotation;

            // Pega o componente de Colisor (formato de cápsula) que envolve o jogador.
            capsule = GetComponent<CapsuleCollider>();

            // Prepara o componente de Som para tocar os passos.
            audioSource = GetComponent<AudioSource>();
            audioSource.clip = audioClipWalking; // Começa com o som de caminhada.
            audioSource.loop = true; // Faz o som repetir enquanto o jogador se move.
        }

        /// Função da Unity que avisa se o jogador está encostando em algo (como o chão).
        private void OnCollisionStay()
        {
            // Bounds são os limites da caixa/cápsula do jogador.
            Bounds bounds = capsule.bounds;
            Vector3 extents = bounds.extents;
            // O raio é um pouco menor que o boneco para não dar erro ao bater em paredes.
            float radius = extents.x - 0.01f;

            // Atira uma "esfera invisível" para baixo. Se ela bater em algo, significa que o chão está logo ali.
            Physics.SphereCastNonAlloc(bounds.center, radius, Vector3.down,
                groundHits, extents.y - radius * 0.5f, ~0, QueryTriggerInteraction.Ignore);

            // Se a esfera bater em algo que não seja o próprio jogador...
            if (!groundHits.Any(hit => hit.collider != null && hit.collider != capsule))
                return; // Se não bateu em nada, o jogador está no ar.

            // Limpa a lista de batidas para a próxima verificação.
            for (var i = 0; i < groundHits.Length; i++)
                groundHits[i] = new RaycastHit();

            // Marca que o jogador está pisando no chão.
            grounded = true;
        }

        // FixedUpdate é o Update focado em FÍSICA. Ele roda em intervalos de tempo fixos (ex: 50x por segundo).
        protected override void FixedUpdate()
        {
            // Chama a função que realmente faz o Rigidbody se mover baseado nas teclas WASD.
            MoveCharacter();

            // Reseta a variável do chão. Se ele continuar no chão, o OnCollisionStay vai marcar como true de novo no próximo frame.
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

        /// <summary>
        /// Calcula e aplica o movimento ao personagem.
        /// </summary>
        private void MoveCharacter()
        {
            #region Calculate Movement Velocity

            // Pergunta ao script do Personagem: "Quais teclas (WASD) o jogador está apertando agora?"
            Vector2 frameInput = playerCharacter.GetInputMovement();
            // X é pros lados, Y (no input 2D) vira o Z (pra frente/trás) no mundo 3D.
            var movement = new Vector3(frameInput.x, 0.0f, frameInput.y);

            // Checa no script do personagem se ele está no estado de CORRER (segurando shift).
            if (playerCharacter.IsRunning())
                movement *= speedRunning; // Multiplica pela velocidade maior.
            else
            {
                movement *= speedWalking; // Multiplica pela velocidade de caminhada.
            }

            // IMPORTANTE: transform.TransformDirection faz com que "pra frente" seja para onde o boneco está olhando.
            // Sem isso, apertar 'W' moveria o jogador sempre para o Norte do mapa, independente de onde ele olhasse.
            movement = transform.TransformDirection(movement);

            #endregion

            // Aplica o movimento final ao Rigidbody. 
            // O Y fica em 0.0f porque quem cuida da gravidade (cair) é o próprio motor de física da Unity.
            Velocity = new Vector3(movement.x, 0.0f, movement.z);
        }

        /// <summary>
        /// Toca os sons de passos.
        /// </summary>
        private void PlayFootstepSounds()
        {
            // Se estiver no chão E a velocidade for maior que quase zero (0.1f)...
            if (grounded && rigidBody.linearVelocity.sqrMagnitude > 0.1f)
            {
                // Escolhe o som: se estiver correndo, som de corrida. Senão, som de caminhada.
                audioSource.clip = playerCharacter.IsRunning() ? audioClipRunning : audioClipWalking;

                // Se o som já não estiver tocando, dá o Play.
                if (!audioSource.isPlaying)
                    audioSource.Play();
            }
            // Se parou de andar ou saiu do chão (pulo/queda), pausa o som.
            else if (audioSource.isPlaying)
                audioSource.Pause();
        }

        #endregion
    }
}