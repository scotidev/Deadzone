using UnityEngine;
using UnityEngine.AI;
using InfimaGames.LowPolyShooterPack;
using Deadzone.Interfaces;
using Deadzone.UI;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// BearTrap buildable item. Places a bear trap in the world.
    /// </summary>
    public class BearTrap : ItemBehaviour
    {

        #region SERIALIZED FIELDS

        [SerializeField] private BuildableDataSO bearTrapData;
        [SerializeField] private Sprite hudIcon;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip equipClip;
        [SerializeField] private float equipVolume = 1f;
        [SerializeField] private AudioClip placementClip;
        [SerializeField] private float placementVolume = 1f;
        [SerializeField] private AudioClip triggerClip;
        [SerializeField] private float triggerVolume = 1f;

        [Header("Trap Activation")]
        [SerializeField] private GameObject closedTrapPrefab;
        [SerializeField] private float stunDurationSeconds = 3f;
        [SerializeField] private float closedTrapDestructionTimeSeconds = 10f;

        #endregion

        #region FIELDS

        private IAudioManagerService audioService;
        private bool hasTriggered = false;
        private bool isPlaced = false;
        private GameObject activeTrapVisual;
        private System.Collections.Generic.HashSet<EnemyBase> stubbedEnemies;

        #endregion

        #region UNITY

        private void Awake()
        {
            audioService = ServiceLocator.Current.Get<IAudioManagerService>();
            stubbedEnemies = new System.Collections.Generic.HashSet<EnemyBase>();
            activeTrapVisual = gameObject;
        }

        private void OnTriggerEnter(Collider collider)
        {
            // Check if trap has been placed in the world (not in player's hand)
            if (!isPlaced) {
                return;
            }

            // Check if trap has already triggered
            if (hasTriggered)
            {
                return;
            }

            // Attempt to get EnemyBase component from the collider
            EnemyBase enemy = collider.GetComponent<EnemyBase>();
            if (enemy == null)
            {
                return;
            }

            // Boss immunity check: don't stun ZombieBoss
            // Use type name checking to avoid direct reference issues
            if (enemy.GetType().Name == "ZombieBoss")
            {
                return;
            }

            // Check if this enemy is already stunned by this trap
            if (stubbedEnemies.Contains(enemy))
            {
                return;
            }

            // Apply damage from trap data
            if (bearTrapData != null)
            {
                enemy.TakeDamage(bearTrapData.Damage);
            }
            else
            {
                Debug.LogWarning("[BearTrap] bearTrapData is null!");
            }

            // Apply stun effect
            ApplyStun(enemy);

            // Play trigger sound and change visual (only once per trap placement)
            if (!hasTriggered)
            {
                PlayTriggerSound();
                hasTriggered = true;
                ChangeVisualState();
            }
        }

        /// <summary>
        /// Applies stun effect to an enemy by disabling movement for a duration.
        /// First principle: We disable the NavMeshAgent via EnemyFollow.SetMovementEnabled(false)
        /// which stops the agent from moving towards the player. We also zero out velocity
        /// (momentum) so the enemy stops IMMEDIATELY without drifting.
        /// We also set the stun lock flag so EnemyAttack cannot re-enable movement during stun.
        /// After the stun duration expires, we re-enable it so the enemy can resume chasing.
        /// </summary>
        private void ApplyStun(EnemyBase enemy)
        {
            EnemyFollow enemyFollow = enemy.GetComponent<EnemyFollow>();
            if (enemyFollow == null)
            {
                Debug.LogWarning($"[BearTrap] {enemy.gameObject.name} has no EnemyFollow component!");
                return;
            }

            // Track this enemy as stunned by this trap
            stubbedEnemies.Add(enemy);

            // Set stun lock - prevents EnemyAttack from re-enabling movement
            enemyFollow.SetStunned(true);

            // Zero out NavMeshAgent velocity immediately (stops momentum/drifting)
            NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.velocity = Vector3.zero;
            }
            else
            {
                Debug.LogWarning($"[BearTrap] {enemy.gameObject.name} has no NavMeshAgent!");
            }

            // Also zero Rigidbody velocity if it exists (and not kinematic)
            Rigidbody rb = enemy.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Only set velocity for non-kinematic bodies
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                }
            }

            // Disable movement
            enemyFollow.SetMovementEnabled(false);

            // Start coroutine to re-enable movement after stun duration
            StartCoroutine(StunCoroutine(enemy, enemyFollow));
        }

        /// <summary>
        /// Coroutine that waits for the stun duration then re-enables enemy movement.
        /// First principle: We use WaitForSeconds to suspend execution for stunDurationSeconds,
        /// then re-enable movement so the enemy AI can resume normal behavior.
        /// We also clear the stun lock flag so EnemyAttack can control movement again.
        /// Important: We capture the GameObject reference upfront and verify it still exists
        /// before accessing any component (enemy, enemyFollow, Agent, etc.). This prevents
        /// MissingReferenceException from zombie object references after the enemy dies.
        /// </summary>
        private System.Collections.IEnumerator StunCoroutine(EnemyBase enemy, EnemyFollow enemyFollow)
        {
            // Capture the GameObject reference immediately — this is what we check
            // instead of the component references, since destroyed Unity objects
            // are not null in C# but become zombie objects that throw on access.
            GameObject enemyGO = enemy != null ? enemy.gameObject : null;

            yield return new WaitForSeconds(stunDurationSeconds);

            // Guard: Check if the GameObject still exists before touching ANY component.
            // TryGetComponent is safe even on destroyed objects — it returns null.
            // We do NOT use enemyFollow directly because it may already be a zombie.
            if (enemyGO != null && !ReferenceEquals(enemyGO, null))
            {
                enemyGO.TryGetComponent(out EnemyFollow follow);
                if (follow != null)
                {
                    follow.SetStunned(false);
                    follow.SetMovementEnabled(true);
                }
                stubbedEnemies.Remove(enemy);
            }
            else
            {
                // Enemy was destroyed during stun — remove from tracking
                if (!ReferenceEquals(enemy, null))
                {
                    stubbedEnemies.Remove(enemy);
                }
            }
        }

/// <summary>
        /// Changes the visual state of the trap from open to closed.
        /// Swaps the active trap GameObject with the closed model prefab.
        /// Instantiates the closed trap WITHOUT a parent so it remains visible
        /// even if the open trap (parent) is deactivated.
        /// Automatically destroys the closed trap after a delay to clean up the scene.
        /// IMPORTANT: We only disable the Renderer, NOT the GameObject, so the coroutine can continue!
        /// </summary>
        private void ChangeVisualState()
        {
            if (closedTrapPrefab == null)
            {
                return;
            }

            Renderer renderer = activeTrapVisual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }

            GameObject closedTrap = Instantiate(
                closedTrapPrefab,
                transform.position,
                transform.rotation
            );

            activeTrapVisual = closedTrap;
            closedTrap.SetActive(true);

            Destroy(closedTrap, closedTrapDestructionTimeSeconds);
        }

        /// <summary>
        /// Sets whether this trap has been placed in the world.
        /// When false (in player's hand), OnTriggerEnter will be ignored.
        /// When true (placed), traps will activate when enemies step on them.
        /// </summary>
        public void SetPlaced(bool placed) {
            isPlaced = placed;
        }

        #endregion

        #region ITEM BEHAVIOUR IMPLEMENTATION

        public override string GetItemID()
        {
            if (bearTrapData == null)
            {
                Debug.LogWarning("[BearTrap] bearTrapData is null!", gameObject);
                return "beartrap_null";
            }
            return bearTrapData.ItemID;
        }

        public override string GetDisplayName()
        {
            if (bearTrapData == null) return "Unknown";
            return bearTrapData.ItemName;
        }

        public override Sprite GetIcon()
        {
            if (hudIcon == null)
            {
                Debug.LogWarning("[BearTrap] hudIcon is null!", gameObject);
                return null;
            }
            return hudIcon;
        }

        /// <summary>
        /// Called when player selects this item (key 8).
        /// Start placement mode (ghost preview appears).
        /// </summary>
        public override void OnSelected()
        {
            PlayEquipSound();
            if (BuildingController.Instance != null && bearTrapData != null)
            {
                BuildingController.Instance.StartPlacement(bearTrapData);
            }
        }

        /// <summary>
        /// Called when player selects another item.
        /// Cancel placement mode.
        /// </summary>
        public override void OnDeselected()
        {
            if (BuildingController.Instance != null && BuildingController.Instance.IsPlacing)
            {
                BuildingController.Instance.CancelPlacement();
            }
        }

        /// <summary>
        /// NORMAL use: Place bear trap with normal damage.
        /// Placement logic is handled by BuildingController.
        /// This method is here for interface compliance.
        /// </summary>
        public override void OnUse()
        {
            if (!CanBeUsed())
            {
                return;
            }
        }

        /// <summary>
        /// Check if bear trap is unlocked AND has quantity in inventory.
        /// </summary>
        public override bool CanBeUsed() {
            if (PlayerProgress.Instance == null) {
                return false;
            }

            bool isUnlocked = PlayerProgress.Instance.IsItemUnlocked(GetItemID());
            int quantity = PlayerProgress.Instance.GetBuildableQuantity(GetItemID());
            if (isUnlocked && quantity <= 0)
                FeedbackMessageUI.Instance?.Show();
            return isUnlocked && quantity > 0;
        }

        #endregion

        #region AUDIO

        public void PlayEquipSound()
        {
            if (equipClip != null && audioService != null)
            {
                audioService.PlaySFX2D(equipClip, equipVolume);
            }
        }

        public void PlayPlacementSound()
        {
            if (placementClip != null && audioService != null)
            {
                audioService.PlaySFX2D(placementClip, placementVolume);
            }
        }

        public void PlayTriggerSound()
        {
            if (triggerClip != null && audioService != null)
            {
                audioService.PlaySFX3D(triggerClip, transform.position, triggerVolume);
            }
        }

        #endregion
    }
}
