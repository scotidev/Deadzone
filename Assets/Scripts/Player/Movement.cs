// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack {
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

        [Header("Surface Traversal")]
        [Tooltip("Maximum walkable slope angle in degrees. Surfaces above this are treated as walls.")]
        [SerializeField]
        private float walkableSlopeAngle = 55.0f;

        [Tooltip("Adds a small downward velocity while grounded to keep contact on stairs and ramps.")]
        [SerializeField]
        private float groundedStickForce = 2.0f;

        [Tooltip("Extra distance for the ground probe to detect stairs and ramps more reliably.")]
        [SerializeField]
        private float groundProbeExtraDistance = 0.08f;

        [Header("Step Assist")]
        [Tooltip("Maximum vertical height that the character can step over.")]
        [SerializeField]
        private float stepMaxHeight = 0.35f;

        [Tooltip("How far ahead we probe for a step in front of the character.")]
        [SerializeField]
        private float stepCheckDistance = 0.35f;

        [Tooltip("How fast the character is lifted while climbing a detected step.")]
        [SerializeField]
        private float stepClimbSpeed = 6.0f;

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
        /// Stores the current walkable ground normal used to align movement on slopes.
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

        /// Checks if the character is on the ground.
        private void OnCollisionStay() {
            //Bounds.
            Bounds bounds = capsule.bounds;
            //Extents.
            Vector3 extents = bounds.extents;
            //Radius.
            float radius = extents.x - 0.01f;
            // The cast distance reaches slightly below the collider to avoid losing ground contact on edges and stair steps.
            float castDistance = extents.y - radius * 0.5f + groundProbeExtraDistance;

            //Cast. This checks whether there is indeed ground, or not.
            Physics.SphereCastNonAlloc(bounds.center, radius, Vector3.down,
                groundHits, castDistance, ~0, QueryTriggerInteraction.Ignore);

            // We select the closest walkable hit because its normal best represents the floor directly below the player.
            bool foundWalkableGround = false;
            float closestDistance = float.MaxValue;
            Vector3 closestNormal = Vector3.up;

            for (int i = 0; i < groundHits.Length; i++) {
                RaycastHit hit = groundHits[i];

                if (hit.collider == null || hit.collider == capsule)
                    continue;

                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                if (slopeAngle > walkableSlopeAngle)
                    continue;

                if (hit.distance < closestDistance) {
                    closestDistance = hit.distance;
                    closestNormal = hit.normal;
                    foundWalkableGround = true;
                }
            }

            //We can ignore the rest if we don't have any proper walkable hits.
            if (!foundWalkableGround)
                return;

            //Store RaycastHits.
            for (var i = 0; i < groundHits.Length; i++)
                groundHits[i] = new RaycastHit();

            //Set grounded. Now we know for sure that we're grounded.
            grounded = true;
            // Save the floor normal so movement can be projected along stairs and ramps.
            groundNormal = closestNormal;
        }

        protected override void FixedUpdate() {
            //Move.
            MoveCharacter();

            //Unground.
            grounded = false;
            // Reset normal to an upright default for the next physics step when no floor is detected.
            groundNormal = Vector3.up;
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

            // When grounded, projecting the desired velocity onto the floor plane removes the component that fights the slope.
            if (grounded)
                movement = Vector3.ProjectOnPlane(movement, groundNormal);

            #endregion

            // Keeping a small downward velocity helps the rigidbody stay attached to stairs and ramps while descending.
            float verticalVelocity = Velocity.y;
            if (grounded && verticalVelocity < 0.0f)
                verticalVelocity = -groundedStickForce;

            Velocity = new Vector3(movement.x, verticalVelocity, movement.z);

            // Step assist checks two heights in front of the capsule: blocked at foot level and clear at upper level means climb.
            TryClimbStep();

            if (grounded && playerCharacter.IsJumping() && Time.time - lastJumpTime >= 0.5f) {
                lastJumpTime = Time.time;
                rigidBody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
        }

        /// <summary>
        /// Attempts to climb a stair step by lifting the rigidbody when a low obstacle is found and upper space is clear.
        /// </summary>
        private void TryClimbStep() {
            // We only attempt a step while grounded, moving horizontally and not actively jumping.
            if (!grounded || playerCharacter.IsJumping())
                return;

            // We read raw movement input because collision can zero-out rigidbody velocity right at the first step.
            Vector2 frameInput = playerCharacter.GetInputMovement();
            if (frameInput.sqrMagnitude < 0.01f)
                return;

            // We transform local input into world direction so probes point exactly where the player is trying to walk.
            Vector3 direction = transform.TransformDirection(new Vector3(frameInput.x, 0.0f, frameInput.y)).normalized;
            Bounds bounds = capsule.bounds;

            // Lower probe starts a little above the floor to avoid hitting tiny seams and to catch the first riser reliably.
            Vector3 lowerOrigin = new Vector3(bounds.center.x, bounds.min.y + 0.08f, bounds.center.z);
            // Upper probe starts above step height to confirm there is free space where the body should move.
            Vector3 upperOrigin = lowerOrigin + Vector3.up * stepMaxHeight;

            // Slightly smaller than capsule radius to avoid false positives at edges while still detecting stair fronts.
            float probeRadius = Mathf.Max(0.01f, capsule.radius * 0.7f);
            float probeDistance = stepCheckDistance + probeRadius;

            bool lowerBlocked = Physics.SphereCast(lowerOrigin, probeRadius, direction, out RaycastHit lowerHit, probeDistance, ~0, QueryTriggerInteraction.Ignore);
            if (!lowerBlocked || lowerHit.collider == null || lowerHit.collider == capsule)
                return;

            bool upperBlocked = Physics.SphereCast(upperOrigin, probeRadius, direction, out RaycastHit upperHit, probeDistance, ~0, QueryTriggerInteraction.Ignore);
            if (upperBlocked && upperHit.collider != null && upperHit.collider != capsule)
                return;

            // Vertical lift uses fixed delta time for stable stair climbing independent from frame rate.
            Vector3 stepOffset = Vector3.up * (stepClimbSpeed * Time.fixedDeltaTime);
            rigidBody.MovePosition(rigidBody.position + stepOffset);
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
