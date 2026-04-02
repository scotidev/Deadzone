// Copyright 2021, Infima Games. All Rights Reserved.

using System.Linq;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Controla a locomoção do personagem usando Rigidbody.
    /// 
    /// Nesta versão, além da movimentação base, foram adicionados:
    /// - leitura robusta de chão (ground probe);
    /// - tratamento de inclinação (slope) para reduzir deslizamento;
    /// - subida de degrau (stair stepping) sem transformar escada em rampa contínua.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class Movement : MovementBehaviour {
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

        [Tooltip("How fast the player moves while aiming."), SerializeField]
        private float speedAiming = 3.0f;

        [Header("Jump")]
        [Tooltip("Jump Strength, values between 4 and 8 are recommended.")]
        [SerializeField]
        private float jumpForce = 5.0f;

        [Header("Grounding")]

        [Tooltip("Extra distance used to probe the ground below the capsule.")]
        [SerializeField]
        // Distância extra para "enxergar" o chão logo abaixo da cápsula.
        // Princípio: ray/sphere cast detecta contato antes de perder o grounded por pequenas irregularidades.
        private float groundProbeDistance = 0.2f;

        [Tooltip("Maximum walkable slope angle.")]
        [SerializeField]
        // Ângulo máximo que consideramos caminhável. Acima disso, não tratamos como chão estável.
        // Princípio: normal da superfície define se é "chão" ou "parede" para locomoção.
        private float maxGroundAngle = 60.0f;

        [Tooltip("Keeps the body glued to the ground while grounded.")]
        [SerializeField]
        // Força para manter o corpo aderido ao chão quando grounded.
        // Princípio: evita micro-quiques em descidas e transições de malha.
        private float groundStickForce = 25.0f;

        [Tooltip("Extra damping applied when idle on slopes to prevent sliding.")]
        [SerializeField]
        // Amortecimento quando parado em ladeira.
        // Princípio: remove energia horizontal residual que gera escorregamento.
        private float slopeIdleDamping = 8.0f;

        [Tooltip("How strongly downhill gravity is canceled on walkable slopes.")]
        [SerializeField]
        // Compensa a componente da gravidade ao longo do plano inclinado.
        // Princípio: gravidade pode ser decomposta em "normal" + "tangencial"; a tangencial causa slide.
        private float slopeAntiSlide = 1.0f;

        [Header("Stairs")]

        [Tooltip("Enable stair stepping when colliding with small ledges.")]
        [SerializeField]
        // Liga/desliga a lógica de step-up (subir degrau).
        private bool stairStepping = true;

        [Tooltip("Maximum step height that can be climbed.")]
        [SerializeField]
        // Equivalente conceitual ao step offset: altura máxima de degrau que pode ser vencida.
        private float maxStepHeight = 0.35f;

        [Tooltip("Forward distance used to detect step obstacles.")]
        [SerializeField]
        // Distância de checagem frontal para detectar o "espelho" do degrau.
        private float stepCheckDistance = 0.35f;

        [Tooltip("How smoothly the body is moved up while climbing steps.")]
        [SerializeField]
        // Limite de elevação por frame de física para suavizar a subida (menos tranco/motion sickness).
        private float stepSmooth = 0.12f;

        #endregion

        #region PROPERTIES

        //Velocity.
        private Vector3 Velocity {
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
        /// Current ground normal.
        /// </summary>
        private Vector3 groundNormal = Vector3.up;

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

        /// <summary>
        /// Moment (in seconds) since the last jump. This is used to calculate the cooldown between jumps, preventing jump spamming.
        /// </summary>
        private float lastJumpTime = -1f;

        #endregion

        #region UNITY FUNCTIONS

        /// <summary>
        /// Awake.
        /// </summary>
        protected override void Awake() {
            //Get Player Character.
            playerCharacter = ServiceLocator.Current.Get<IGameModeService>().GetPlayerCharacter();
        }

        /// Initializes the FpsController on start.
        protected override void Start() {
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

        protected override void FixedUpdate() {
            // 1) Primeiro detectamos chão e normal.
            // Princípio: todas as decisões de slope/escada dependem de saber se estamos grounded.
            ProbeGround();

            // 2) Depois aplicamos velocidade de movimento.
            MoveCharacter();

            // 3) Por fim, estabilizamos em chão inclinado.
            if (grounded && Velocity.y <= 0.0f) {
                // Cola no chão para reduzir "saltinhos" ao descer/andar em superfícies irregulares.
                rigidBody.AddForce(-groundNormal * groundStickForce, ForceMode.Acceleration);

                // Remove a gravidade tangencial da ladeira para minimizar escorregamento parado.
                Vector3 slopeGravity = Vector3.ProjectOnPlane(Physics.gravity, groundNormal);
                rigidBody.AddForce(-slopeGravity * slopeAntiSlide, ForceMode.Acceleration);
            }
        }

        /// Moves the camera to the character, processes jumping and plays sounds every frame.
        protected override void Update() {
            //Get the equipped weapon!
            equippedWeapon = playerCharacter.GetInventory().GetEquipped();

            //Play Sounds!
            PlayFootstepSounds();
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Faz a leitura do chão usando SphereCast e define:
        /// - se o personagem está grounded;
        /// - qual a normal da superfície de apoio.
        /// 
        /// Regra: só considera superfície com inclinação até <see cref="maxGroundAngle"/>.
        /// </summary>
        private void ProbeGround() {
            Bounds bounds = capsule.bounds;
            Vector3 extents = bounds.extents;
            float radius = Mathf.Max(0.01f, extents.x - 0.02f);
            float castDistance = extents.y - radius + groundProbeDistance;

            Physics.SphereCastNonAlloc(
                bounds.center,
                radius,
                Vector3.down,
                groundHits,
                castDistance,
                ~0,
                QueryTriggerInteraction.Ignore
            );

            grounded = false;
            groundNormal = Vector3.up;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < groundHits.Length; i++) {
                RaycastHit hit = groundHits[i];

                if (hit.collider == null || hit.collider == capsule)
                    continue;

                // Usamos a normal para calcular o ângulo da superfície.
                float angle = Vector3.Angle(hit.normal, Vector3.up);
                if (angle > maxGroundAngle)
                    continue;

                // Escolhe o hit válido mais próximo para obter uma normal estável do chão.
                if (hit.distance < bestDistance) {
                    bestDistance = hit.distance;
                    groundNormal = hit.normal;
                    grounded = true;
                }
            }

            // Limpeza do buffer non-alloc para não reaproveitar lixo de frame anterior.
            for (int i = 0; i < groundHits.Length; i++)
                groundHits[i] = new RaycastHit();
        }

        private void MoveCharacter() {
            #region Calculate Movement Velocity

            //Get Movement Input!
            Vector2 frameInput = playerCharacter.GetInputMovement();
            //Calculate local-space direction by using the player's input.
            var movement = new Vector3(frameInput.x, 0.0f, frameInput.y);

            //Speed calculation based on character state.
            if (playerCharacter.IsAiming()) {
                //Multiply by the aiming speed (reduced movement).
                movement *= speedAiming;
            }
            else if (playerCharacter.IsRunning()) {
                //Multiply by the running speed.
                movement *= speedRunning;
            }
            else {
                //Multiply by the normal walking speed.
                movement *= speedWalking;
            }

            //World space velocity calculation. This allows us to add it to the rigidbody's velocity properly.
            movement = transform.TransformDirection(movement);

            #endregion

            // Quando grounded, projetamos o movimento no plano do chão.
            // Princípio: evita ganhar/perder velocidade indevida em superfícies inclinadas.
            Vector3 desiredMovement = grounded ? Vector3.ProjectOnPlane(movement, groundNormal) : movement;

            // Tentativa de step-up somente quando há input e estamos no chão.
            if (stairStepping && grounded && frameInput.sqrMagnitude > 0.001f && Velocity.y <= 0.1f)
                TryStepUp(desiredMovement);

            Velocity = new Vector3(desiredMovement.x, Velocity.y, desiredMovement.z);

            // Anti-slide quando parado na ladeira.
            if (grounded && frameInput.sqrMagnitude <= 0.0001f && !playerCharacter.IsJumping()) {
                Vector3 planarVelocity = Vector3.ProjectOnPlane(Velocity, groundNormal);
                Velocity -= planarVelocity * Mathf.Clamp01(Time.fixedDeltaTime * slopeIdleDamping);

                // Se já está quase parado no plano, zera completamente XZ para estabilizar.
                if (Vector3.ProjectOnPlane(Velocity, groundNormal).sqrMagnitude < 0.01f)
                    Velocity = new Vector3(0.0f, Mathf.Min(Velocity.y, 0.0f), 0.0f);
                else
                    Velocity = new Vector3(Velocity.x, Mathf.Min(Velocity.y, 0.0f), Velocity.z);
            }

            if (grounded && playerCharacter.IsJumping() && Time.time - lastJumpTime >= 0.5f) {
                lastJumpTime = Time.time;
                rigidBody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
        }

        /// <summary>
        /// Tenta subir um degrau à frente do personagem.
        /// 
        /// Estratégia básica:
        /// 1) Ray baixo encontra obstáculo frontal (espelho do degrau);
        /// 2) Ray alto confirma espaço livre para passar;
        /// 3) Ray para baixo encontra topo do degrau;
        /// 4) Se altura e ângulo forem válidos, aplica elevação suave.
        /// </summary>
        /// <param name="desiredMovement">Direção de movimento desejada no mundo.</param>
        private void TryStepUp(Vector3 desiredMovement) {
            Vector3 moveDirection = new Vector3(desiredMovement.x, 0.0f, desiredMovement.z);
            if (moveDirection.sqrMagnitude <= 0.0001f)
                return;

            moveDirection.Normalize();

            Bounds bounds = capsule.bounds;
            Vector3 feet = new Vector3(bounds.center.x, bounds.min.y + 0.02f, bounds.center.z);

            // Ray baixo: detecta barreira frontal na altura do pé.
            if (!Physics.Raycast(feet, moveDirection, out RaycastHit lowerHit, stepCheckDistance, ~0, QueryTriggerInteraction.Ignore))
                return;

            // Se for o próprio collider ou já for uma normal muito "de chão", não tratamos como degrau.
            if (lowerHit.collider == capsule || lowerHit.normal.y > 0.1f)
                return;

            // Ray alto: precisa estar livre para caber a cápsula ao subir.
            Vector3 upperOrigin = feet + Vector3.up * maxStepHeight;
            if (Physics.Raycast(upperOrigin, moveDirection, stepCheckDistance, ~0, QueryTriggerInteraction.Ignore))
                return;

            // Ray para baixo após o obstáculo: encontra o topo real do degrau.
            Vector3 stepProbeOrigin = upperOrigin + moveDirection * stepCheckDistance;
            if (!Physics.Raycast(stepProbeOrigin, Vector3.down, out RaycastHit stepHit, maxStepHeight + 0.2f, ~0,
                    QueryTriggerInteraction.Ignore))
                return;

            if (stepHit.collider == capsule)
                return;

            float stepAngle = Vector3.Angle(stepHit.normal, Vector3.up);
            if (stepAngle > maxGroundAngle)
                return;

            float currentFoot = feet.y;
            float delta = stepHit.point.y - currentFoot;
            if (delta <= 0.01f || delta > maxStepHeight)
                return;

            // Elevação limitada por stepSmooth para evitar tranco visual.
            float stepDelta = Mathf.Min(delta, stepSmooth);
            rigidBody.MovePosition(rigidBody.position + Vector3.up * stepDelta);
        }

        /// <summary>
        /// Plays Footstep Sounds. This code is slightly old, so may not be great, but it functions alright-y!
        /// </summary>
        private void PlayFootstepSounds() {
            //Check if we're moving on the ground. We don't need footsteps in the air.
            if (grounded && rigidBody.linearVelocity.sqrMagnitude > 0.1f) {
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