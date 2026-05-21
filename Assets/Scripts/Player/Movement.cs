// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Controls character movement using Rigidbody.
    /// Ensures the player interacts properly with the ground, including slopes and stairs, and handles jumping and crouching.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class Movement : MovementBehaviour {

        #region SERIALIZED FIELDS

        [Header("Audio Clips")]

        [SerializeField] private AudioClip audioClipWalking;
        [SerializeField] private AudioClip audioClipRunning;

        [Header("Speeds")]

        [SerializeField] private float speedWalking = 5.0f;
        [SerializeField] private float speedRunning = 9.0f;
        [SerializeField] private float speedAiming = 3.0f;
        [SerializeField] private float speedCrouching = 1.8f;

        [Header("Crouch")]

        [SerializeField] private float crouchHeightMultiplier = 0.5f;
        [SerializeField] private float crouchCameraOffset = -0.25f;
        [SerializeField] private float crouchTransitionSpeed = 8.0f;
        [SerializeField] private Transform characterRootTransform;

        [Header("Jump")]

        [SerializeField] private float jumpForce = 5.0f;

        [Header("Grounding")]

        [SerializeField] private float groundProbeDistance = 0.2f;
        [SerializeField] private float maxGroundAngle = 60.0f;
        [SerializeField] private float groundStickForce = 25.0f;

        [Tooltip("Extra damping applied when idle on slopes to prevent sliding.")]
        [SerializeField] private float slopeIdleDamping = 8.0f;

        [Tooltip("How strongly downhill gravity is canceled on walkable slopes.")]
        [SerializeField] private float slopeAntiSlide = 1.0f;

        [Header("Stairs")]

        [SerializeField] private bool stairStepping = true;
        [SerializeField] private float maxStepHeight = 0.35f;
        [SerializeField] private float stepCheckDistance = 0.35f;
        [SerializeField] private float stepSmooth = 0.12f;

        #endregion

        #region FIELDS

        private Rigidbody rigidBody;
        private CapsuleCollider capsule;
        private CharacterBehaviour playerCharacter;
        private AudioSource audioSource;

        private Vector3 groundNormal = Vector3.up;
        private Vector3 steepNormal = Vector3.up;
        private bool grounded;
        private bool touchingSteepSlope;
        private float lastJumpTime = -1f;

        private readonly RaycastHit[] groundHits = new RaycastHit[8];

        private float originalCapsuleHeight;
        private float originalCapsuleCenterY;
        private float targetCapsuleHeight;
        private float targetCapsuleCenterY;

        private Transform cameraTransform;
        private float originalCameraY;
        private float targetCameraY;

        private float originalRootY;
        private float targetRootY;

        #endregion

        #region PROPERTIES

        private Vector3 Velocity {
            get => rigidBody.linearVelocity;
            set => rigidBody.linearVelocity = value;
        }

        #endregion

        #region UNITY

        protected override void Awake() {
            playerCharacter = ServiceLocator.Current.Get<IGameModeService>().GetPlayerCharacter();
        }

        protected override void Start() {
            rigidBody = GetComponent<Rigidbody>();
            rigidBody.constraints = RigidbodyConstraints.FreezeRotation;

            capsule = GetComponent<CapsuleCollider>();

            audioSource = GetComponent<AudioSource>();
            audioSource.volume = ServiceLocator.Current.Get<IAudioManagerService>().GetSFXVolume();
            audioSource.clip = audioClipWalking;
            audioSource.loop = true;

            originalCapsuleHeight = capsule.height;
            originalCapsuleCenterY = capsule.center.y;
            targetCapsuleHeight = originalCapsuleHeight;
            targetCapsuleCenterY = originalCapsuleCenterY;

            cameraTransform = playerCharacter.GetCameraWorld().transform;
            originalCameraY = cameraTransform.localPosition.y;
            targetCameraY = originalCameraY;

            originalRootY = characterRootTransform.localPosition.y;
            targetRootY = originalRootY;
        }

        protected override void FixedUpdate() {
            ProbeGround();
            MoveCharacter();

            if (grounded && Velocity.y <= 0.0f) {
                rigidBody.AddForce(-groundNormal * groundStickForce, ForceMode.Acceleration);

                Vector3 slopeGravity = Vector3.ProjectOnPlane(Physics.gravity, groundNormal);
                rigidBody.AddForce(-slopeGravity * slopeAntiSlide, ForceMode.Acceleration);
            }
        }

        protected override void Update() {
            ProcessCrouch();
            PlayFootstepSounds();
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Uses SphereCast to probe the ground and determine if
        /// The character is grounded and what the ground normal is (surface orientation).
        /// Rule: only considers surfaces with an angle up to <see cref="maxGroundAngle"/>.
        /// </summary>
        private void ProbeGround() {
            Bounds bounds = capsule.bounds;
            Vector3 extents = bounds.extents;
            // O raio da esfera deve ser um pouco menor que o da cápsula para evitar colisões fantasmas nas quinas.
            float radius = Mathf.Max(0.01f, extents.x - 0.02f);
            // A distância do cast cobre a altura da cápsula mais a margem de detecção do chão.
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
            touchingSteepSlope = false;
            groundNormal = Vector3.up;
            steepNormal = Vector3.up;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < groundHits.Length; i++) {
                RaycastHit hit = groundHits[i];

                if (hit.collider == null || hit.collider == capsule)
                    continue;

                // Calculamos o ângulo entre a normal da superfície e o vetor 'Up' (Cima).
                // O princípio aqui é que 0 graus é plano e 90 graus é uma parede vertical.
                float angle = Vector3.Angle(hit.normal, Vector3.up);
                
                if (angle <= maxGroundAngle) {
                    // Se o ângulo for menor que o máximo permitido, consideramos como chão firme.
                    if (hit.distance < bestDistance) {
                        bestDistance = hit.distance;
                        groundNormal = hit.normal;
                        grounded = true;
                    }
                } else {
                    // Se o ângulo for maior, marcamos que estamos tocando uma inclinação íngreme.
                    // Isso é essencial para bloquear o "climbing" (escalada) involuntário.
                    touchingSteepSlope = true;
                    steepNormal = hit.normal;
                }
            }

            for (int i = 0; i < groundHits.Length; i++)
                groundHits[i] = new RaycastHit();
        }

        /// <summary>
        /// Method responsible for moving the character based on player input and current state (walking, running, aiming, crouching).
        /// Also handles jumping and avoidance of sliding on slopes when idle.
        /// </summary>
        private void MoveCharacter() {
            Vector2 frameInput = playerCharacter.GetInputMovement();

            var movement = new Vector3(frameInput.x, 0.0f, frameInput.y);

            if (playerCharacter.IsAiming()) {
                movement *= speedAiming;
            } else if (playerCharacter.IsCrouching()) {
                movement *= speedCrouching;
            } else if (playerCharacter.IsRunning()) {
                movement *= speedRunning;
            } else {
                movement *= speedWalking;
            }

            movement = transform.TransformDirection(movement);

            // Se estivermos tocando uma inclinação muito íngreme, precisamos filtrar o movimento.
            // O princípio é tratar a inclinação como uma parede horizontal para impedir que o personagem "suba" nela.
            if (touchingSteepSlope) {
                // Criamos um vetor horizontal baseado na inclinação da montanha. 
                // Isso transforma a montanha em uma "parede virtual" para o cálculo de movimento.
                Vector3 wallNormal = new Vector3(steepNormal.x, 0, steepNormal.z).normalized;
                
                // O Produto Escalar (Dot Product) nos diz se estamos andando na direção da montanha.
                // Se o valor for menor que zero, significa que o movimento aponta "para dentro" da superfície.
                if (Vector3.Dot(movement, wallNormal) < 0) {
                    // Projetamos o movimento no plano da parede. 
                    // Isso remove a componente do movimento que faz o personagem subir a montanha à força.
                    movement = Vector3.ProjectOnPlane(movement, wallNormal);
                }
            }

            Vector3 desiredMovement = grounded ? Vector3.ProjectOnPlane(movement, groundNormal) : movement;

            if (stairStepping && grounded && frameInput.sqrMagnitude > 0.001f && Velocity.y <= 0.1f)
                TryStepUp(desiredMovement);

            Velocity = new Vector3(desiredMovement.x, Velocity.y, desiredMovement.z);

            if (grounded && frameInput.sqrMagnitude <= 0.0001f && !playerCharacter.IsJumping()) {
                Vector3 planarVelocity = Vector3.ProjectOnPlane(Velocity, groundNormal);
                Velocity -= planarVelocity * Mathf.Clamp01(Time.fixedDeltaTime * slopeIdleDamping);

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
        /// Attempts to step up onto a ledge in front of the character.
        /// </summary>
        /// <param name="desiredMovement">Desired movement direction in world space.</param>
        private void TryStepUp(Vector3 desiredMovement) {
            Vector3 moveDirection = new Vector3(desiredMovement.x, 0.0f, desiredMovement.z);

            if (moveDirection.sqrMagnitude <= 0.0001f)
                return;

            moveDirection.Normalize();

            Bounds bounds = capsule.bounds;

            Vector3 feet = new Vector3(bounds.center.x, bounds.min.y + 0.02f, bounds.center.z);
            if (!Physics.Raycast(feet, moveDirection, out RaycastHit lowerHit, stepCheckDistance, ~0, QueryTriggerInteraction.Ignore))
                return;

            if (lowerHit.collider == capsule || lowerHit.normal.y > 0.1f)
                return;

            Vector3 upperOrigin = feet + Vector3.up * maxStepHeight;
            if (Physics.Raycast(upperOrigin, moveDirection, stepCheckDistance, ~0, QueryTriggerInteraction.Ignore))
                return;

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

            float stepDelta = Mathf.Min(delta, stepSmooth);
            rigidBody.MovePosition(rigidBody.position + Vector3.up * stepDelta);
        }

        /// <summary>
        /// Plays Footstep Sounds. This code is slightly old, so may not be great, but it functions alright-y!
        /// </summary>
        private void PlayFootstepSounds() {
            if (grounded && rigidBody.linearVelocity.sqrMagnitude > 0.1f) {
                audioSource.clip = playerCharacter.IsRunning() ? audioClipRunning : audioClipWalking;

                if (!audioSource.isPlaying) {
                    audioSource.volume = ServiceLocator.Current.Get<IAudioManagerService>().GetSFXVolume();
                    audioSource.Play();
                }
            } else if (audioSource.isPlaying)
                audioSource.Pause();
        }

        /// <summary>
        /// Processes the character's crouching view, smoothly.
        /// </summary>
        private void ProcessCrouch() {
            bool isCrouching = playerCharacter.IsCrouching();

            if (isCrouching) {
                targetCapsuleHeight = originalCapsuleHeight * crouchHeightMultiplier;

                float heightDifference = originalCapsuleHeight - targetCapsuleHeight;

                targetCapsuleCenterY = originalCapsuleCenterY - (heightDifference * 0.5f);

                targetRootY = originalRootY + crouchCameraOffset;

                targetCameraY = originalCameraY;
            } else {
                targetCapsuleHeight = originalCapsuleHeight;
                targetCapsuleCenterY = originalCapsuleCenterY;
                targetRootY = originalRootY;
                targetCameraY = originalCameraY;
            }

            capsule.height = Mathf.Lerp(capsule.height, targetCapsuleHeight,
                Time.deltaTime * crouchTransitionSpeed);

            Vector3 center = capsule.center;
            center.y = Mathf.Lerp(center.y, targetCapsuleCenterY,
                Time.deltaTime * crouchTransitionSpeed);
            capsule.center = center;

            if (characterRootTransform != null) {
                Vector3 rootPos = characterRootTransform.localPosition;
                float oldRootY = rootPos.y;

                rootPos.y = Mathf.Lerp(rootPos.y, targetRootY,
                    Time.deltaTime * crouchTransitionSpeed);
                characterRootTransform.localPosition = rootPos;

            }
        }

        #endregion

        #region GIZMOS
        private void OnDrawGizmosSelected() {
            if (capsule == null) capsule = GetComponent<CapsuleCollider>();

            Gizmos.color = grounded ? Color.green : Color.red;
            Bounds bounds = capsule.bounds;
            float radius = Mathf.Max(0.01f, bounds.extents.x - 0.02f);

            Vector3 sphereOrigin = bounds.center + Vector3.down * (bounds.extents.y - radius);
            Gizmos.DrawWireSphere(sphereOrigin, radius);

            Gizmos.DrawLine(sphereOrigin, sphereOrigin + Vector3.down * groundProbeDistance);

            Gizmos.color = Color.yellow;
            Vector3 moveDir = transform.forward;
            Vector3 feet = new Vector3(bounds.center.x, bounds.min.y + 0.02f, bounds.center.z);

            Gizmos.DrawRay(feet, moveDir * stepCheckDistance);

            Vector3 upperOrigin = feet + Vector3.up * maxStepHeight;
            Gizmos.DrawRay(upperOrigin, moveDir * stepCheckDistance);

            Vector3 stepProbeOrigin = upperOrigin + moveDir * stepCheckDistance;
            Gizmos.DrawLine(stepProbeOrigin, stepProbeOrigin + Vector3.down * (maxStepHeight + 0.2f));
        }

        #endregion
    }
}