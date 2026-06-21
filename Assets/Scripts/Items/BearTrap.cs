using UnityEngine;
using UnityEngine.AI;
using InfimaGames.LowPolyShooterPack;
using Deadzone.Interfaces;
using Deadzone.UI;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// BearTrap buildable item. Places a bear trap in the world.
    /// When triggered by an enemy, applies damage and stun.
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

        #region PROPERTIES

        #endregion

        #region EVENTS

        #endregion

        #region CONSTANTS

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
            if (!isPlaced) {
                return;
            }

            if (hasTriggered)
            {
                return;
            }

            EnemyBase enemy = collider.GetComponent<EnemyBase>();
            if (enemy == null)
            {
                return;
            }

            if (enemy.GetType().Name == "ZombieBoss")
            {
                return;
            }

            if (stubbedEnemies.Contains(enemy))
            {
                return;
            }

            if (bearTrapData != null)
            {
                float damage = bearTrapData.Damage;
                if (PlayerProgress.Instance != null)
                {
                    int level = PlayerProgress.Instance.GetItemLevel(GetItemID());
                    damage = bearTrapData.GetDamageAtLevel(level);
                }
                enemy.TakeDamage(damage);
            }
            else
            {
                Debug.LogWarning("[BearTrap] bearTrapData is null!");
            }

            ApplyStun(enemy);

            if (!hasTriggered)
            {
                PlayTriggerSound();
                hasTriggered = true;
                ChangeVisualState();
            }
        }

        /// <summary>
        /// Applies stun effect to an enemy by disabling movement for a duration.
        /// Disables NavMeshAgent via EnemyFollow.SetMovementEnabled(false) and zeros velocity
        /// to stop the enemy immediately. After the stun duration, movement is re-enabled.
        /// </summary>
        private void ApplyStun(EnemyBase enemy)
        {
            EnemyFollow enemyFollow = enemy.GetComponent<EnemyFollow>();
            if (enemyFollow == null)
            {
                Debug.LogWarning($"[BearTrap] {enemy.gameObject.name} has no EnemyFollow component!");
                return;
            }

            stubbedEnemies.Add(enemy);
            enemyFollow.SetStunned(true);

            NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.velocity = Vector3.zero;
            }
            else
            {
                Debug.LogWarning($"[BearTrap] {enemy.gameObject.name} has no NavMeshAgent!");
            }

            Rigidbody rb = enemy.GetComponent<Rigidbody>();
            if (rb != null)
            {
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                }
            }

            enemyFollow.SetMovementEnabled(false);

            StartCoroutine(StunCoroutine(enemy, enemyFollow));
        }

        /// <summary>
        /// Coroutine that waits for the stun duration then re-enables enemy movement.
        /// Captures the GameObject reference upfront and verifies it still exists
        /// before accessing any component to prevent MissingReferenceException.
        /// </summary>
        private System.Collections.IEnumerator StunCoroutine(EnemyBase enemy, EnemyFollow enemyFollow)
        {
            GameObject enemyGO = enemy != null ? enemy.gameObject : null;

            yield return new WaitForSeconds(stunDurationSeconds);

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
                if (!ReferenceEquals(enemy, null))
                {
                    stubbedEnemies.Remove(enemy);
                }
            }
        }

        /// <summary>
        /// Changes the visual state of the trap from open to closed.
        /// Swaps the active trap GameObject with the closed model prefab.
        /// Instantiates without a parent so it remains visible even if the open trap is deactivated.
        /// Automatically destroys the closed trap after a delay to clean up the scene.
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

        #region METHODS

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
        /// Starts placement mode (ghost preview appears).
        /// </summary>
        public override void OnSelected()
        {
            PlayEquipSound();
            if (PlayerProgress.Instance != null)
            {
                string id = GetItemID();
                int total = PlayerProgress.Instance.GetItemTotal(id);
                PlayerProgress.Instance.SetItemCurrent(id, total > 0 ? 1 : 0);
            }
            if (BuildingController.Instance != null && bearTrapData != null)
            {
                BuildingController.Instance.StartPlacement(bearTrapData);
            }
        }

        /// <summary>
        /// Called when player selects another item. Cancels placement mode.
        /// </summary>
        public override void OnDeselected()
        {
            if (BuildingController.Instance != null && BuildingController.Instance.IsPlacing)
            {
                BuildingController.Instance.CancelPlacement();
            }
        }

        /// <summary>
        /// Normal use: Place bear trap with normal damage.
        /// Placement logic is handled by BuildingController.
        /// </summary>
        public override void OnUse()
        {
            if (!CanBeUsed())
            {
                return;
            }
        }

        /// <summary>
        /// Checks if bear trap is unlocked and has quantity in inventory.
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

        #region ANIMATION

        /// <summary>
        /// BearTrap does not need a weapon pose. Keeps hands lowered when equipped.
        /// </summary>
        public override bool KeepHolsteredOnEquip() => true;

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

        #region DEBUG

        #endregion

        #endregion
    }
}
