// Copyright 2021, Infima Games. All Rights Reserved.

using System.Linq;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class Movement : MovementBehaviour
    {
        #region FIELDS SERIALIZED

        [Header("Audio Clips")]
        
        [Tooltip("The audio clip that is played while walking.")]
        [SerializeField]
        private AudioClip audioClipWalking;

        [Tooltip("The audio clip that is played while running.")]
        [SerializeField]
        private AudioClip audioClipRunning;

        [Header("Speeds")]

        [SerializeField]
        private float speedWalking = 5.0f;

        [Tooltip("How fast the player moves while running."), SerializeField]
        private float speedRunning = 9.0f;

        // ==============================================================
        //  [ADICIONADO] jumpForce
        // ==============================================================
        //  AULA: O QUE É ForceMode.Impulse E POR QUE USAR ELE?
        //
        //  Na física do Unity, existem formas diferentes de aplicar força:
        //
        //  ForceMode.Force     → força contínua, cresce devagar (motor de foguete)
        //  ForceMode.Impulse   → força instantânea, aplicada uma só vez (chute numa bola)
        //  ForceMode.Acceleration → ignora a massa do objeto
        //  ForceMode.VelocityChange → muda a velocidade ignorando a massa
        //
        //  Para pulo, queremos um impulso único e instantâneo para cima —
        //  como se o personagem desse um salto. Por isso usamos Impulse.
        //
        //  O valor de jumpForce controla a "força do chute". Valores típicos:
        //  → 4.0 a 5.0 = pulo baixo e rápido
        //  → 6.0 a 8.0 = pulo alto e dramático
        //
        //  [Header] cria um separador visual no Inspector.
        //  [Tooltip] mostra um texto de ajuda ao passar o mouse no Inspector.
        //  [SerializeField] expõe o campo privado para edição no Inspector.
        [Header("Pulo")]
        [Tooltip("Força do impulso vertical aplicada ao pular. Valores entre 4 e 8 são recomendados.")]
        [SerializeField]
        private float jumpForce = 5.0f;

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

        // ==============================================================
        //  [ADICIONADO] lastJumpTime — controla o cooldown entre pulos
        // ==============================================================
        //  AULA: O QUE É Time.time E COMO USAR PARA CRIAR UM COOLDOWN?
        //
        //  "Time.time" é um contador que a Unity mantém automaticamente:
        //  ele guarda quantos SEGUNDOS se passaram desde que o jogo iniciou.
        //  É como um relógio que nunca para, sempre crescendo.
        //
        //  Exemplo real:
        //    Jogo inicia       → Time.time = 0.0
        //    Após 3 segundos   → Time.time = 3.0
        //    Após 10 segundos  → Time.time = 10.0
        //
        //  Para criar um cooldown, usamos uma subtração simples:
        //    "quanto tempo passou desde o último pulo?"
        //    = Time.time - lastJumpTime
        //
        //  Se esse valor for maior que 0.5 (segundos), o cooldown acabou
        //  e o jogador pode pular novamente.
        //
        //  Por que inicializamos com -1f?
        //  Para garantir que no PRIMEIRO frame do jogo, a conta seja:
        //    Time.time(≈0) - lastJumpTime(-1) = 1.0 → maior que 0.5 ✓
        //  Ou seja, o jogador pode pular imediatamente quando o jogo começa,
        //  sem ter que esperar 0.5s antes do primeiro salto.
        /// <summary>
        /// Momento (em segundos) do último pulo realizado. Usado para calcular o cooldown.
        /// </summary>
        private float lastJumpTime = -1f;

        #endregion

        #region UNITY FUNCTIONS

        /// <summary>
        /// Awake.
        /// </summary>
        protected override void Awake()
        {
            //Get Player Character.
            playerCharacter = ServiceLocator.Current.Get<IGameModeService>().GetPlayerCharacter();
        }

        /// Initializes the FpsController on start.
        protected override  void Start()
        {
            //Rigidbody Setup.
            rigidBody = GetComponent<Rigidbody>();
            rigidBody.constraints = RigidbodyConstraints.FreezeRotation;
            //Cache the CapsuleCollider.
            capsule = GetComponent<CapsuleCollider>();

            //Audio Source Setup.
            audioSource = GetComponent<AudioSource>();
            audioSource.clip = audioClipWalking;
            audioSource.loop = true;
        }

        /// Checks if the character is on the ground.
        private void OnCollisionStay()
        {
            //Bounds.
            Bounds bounds = capsule.bounds;
            //Extents.
            Vector3 extents = bounds.extents;
            //Radius.
            float radius = extents.x - 0.01f;
            
            //Cast. This checks whether there is indeed ground, or not.
            Physics.SphereCastNonAlloc(bounds.center, radius, Vector3.down,
                groundHits, extents.y - radius * 0.5f, ~0, QueryTriggerInteraction.Ignore);
            
            //We can ignore the rest if we don't have any proper hits.
            if (!groundHits.Any(hit => hit.collider != null && hit.collider != capsule)) 
                return;
            
            //Store RaycastHits.
            for (var i = 0; i < groundHits.Length; i++)
                groundHits[i] = new RaycastHit();

            //Set grounded. Now we know for sure that we're grounded.
            grounded = true;
        }
			
        protected override void FixedUpdate()
        {
            //Move.
            MoveCharacter();
            
            //Unground.
            grounded = false;
        }

        /// Moves the camera to the character, processes jumping and plays sounds every frame.
        protected override  void Update()
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

            //Get Movement Input!
            Vector2 frameInput = playerCharacter.GetInputMovement();
            //Calculate local-space direction by using the player's input.
            var movement = new Vector3(frameInput.x, 0.0f, frameInput.y);

            //Running speed calculation.
            if(playerCharacter.IsRunning())
                movement *= speedRunning;
            else
            {
                //Multiply by the normal walking speed.
                movement *= speedWalking;
            }

            //World space velocity calculation. This allows us to add it to the rigidbody's velocity properly.
            movement = transform.TransformDirection(movement);

            #endregion

            // ==============================================================
            //  [MODIFICADO] Atualiza a velocidade PRESERVANDO o eixo Y
            // ==============================================================
            //  AULA: POR QUE O CÓDIGO ORIGINAL QUEBRAVA O PULO?
            //
            //  O código original fazia isto:
            //      Velocity = new Vector3(movement.x, 0.0f, movement.z);
            //
            //  O problema está no "0.0f" no eixo Y. A cada frame físico
            //  (50 vezes por segundo), o Y era forçado a zero. Isso significa:
            //
            //  Frame 1: aplicamos impulso de pulo → Y = +5 ✓
            //  Frame 2: MoveCharacter() roda → Y = 0  ✗ (pulo cancelado!)
            //
            //  A correção é simples: ao construir o novo vetor de velocidade,
            //  mantemos o Y que o Rigidbody já tinha calculado:
            //      Velocity = new Vector3(movement.x, Velocity.y, movement.z);
            //
            //  "Velocity.y" lê a velocidade vertical ATUAL do Rigidbody.
            //  Isso inclui tanto o impulso do pulo quanto a gravidade.
            //  Assim a física natural do Unity toma conta da descida — sem
            //  precisar escrever nenhuma lógica de gravidade manual.
            Velocity = new Vector3(movement.x, Velocity.y, movement.z);

            // ==============================================================
            //  [ADICIONADO] Lógica de pulo com cooldown
            // ==============================================================
            //  AULA: POR QUE VERIFICAMOS "grounded" ANTES DE PULAR?
            //
            //  "grounded" é marcado como true em OnCollisionStay(), que é
            //  chamado pela Unity enquanto o personagem está tocando algum
            //  objeto sólido (o chão). No FixedUpdate(), após MoveCharacter(),
            //  ele é resetado para false — e só volta a true no próximo
            //  OnCollisionStay, que acontece no mesmo frame se ainda houver
            //  contato com o chão.
            //
            //  Isso cria um "detector de chão" confiável:
            //  → grounded = true  → personagem está no chão → pode pular
            //  → grounded = false → personagem está no ar  → NÃO pode pular
            //
            //  Sem essa verificação, o jogador poderia pular infinitamente
            //  no ar (o famoso "double jump indesejado").
            //
            //  AULA: COMO O COOLDOWN DE 0.5S FUNCIONA AQUI?
            //
            //  Adicionamos uma terceira condição ao pulo:
            //      Time.time - lastJumpTime >= 0.5f
            //
            //  Veja o fluxo completo:
            //
            //  t = 5.00s → jogador pula
            //              lastJumpTime = 5.00
            //              impulso aplicado ✓
            //
            //  t = 5.10s → jogador está no chão (caiu rápido) e aperta espaço
            //              Time.time(5.10) - lastJumpTime(5.00) = 0.10s < 0.5 ✗
            //              pulo BLOQUEADO pelo cooldown
            //
            //  t = 5.55s → jogador aperta espaço novamente
            //              Time.time(5.55) - lastJumpTime(5.00) = 0.55s ≥ 0.5 ✓
            //              pulo PERMITIDO → lastJumpTime = 5.55
            //
            //  As 3 condições precisam ser ALL TRUE ao mesmo tempo (operador &&):
            //  1. grounded        → está no chão
            //  2. IsJumping()     → espaço está pressionado
            //  3. cooldown vencido → passaram-se 0.5s desde o último pulo
            if (grounded && playerCharacter.IsJumping() && Time.time - lastJumpTime >= 0.5f) {
                // Registra o momento exato deste pulo para o próximo cálculo de cooldown.
                lastJumpTime = Time.time;
                rigidBody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
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