using UnityEngine;
using System.Collections;
using InfimaGames.LowPolyShooterPack;
using Deadzone.Interfaces;
using Deadzone.UI;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Explosive barrel buildable item. Places an explosive barrel in the world.
    /// When shot, triggers a chain reaction explosion with slow-motion effect.
    /// </summary>
    public class ExplosiveBarrel : ItemBehaviour, IDamageable {

        #region SERIALIZED FIELDS

        [Header("Barrel Data")]
        [SerializeField] private BuildableDataSO barrelData;
        [SerializeField] private Sprite hudIcon;

        [Header("Prefabs")]
        [SerializeField] private Transform explosionPrefab;
        [SerializeField] private Transform destroyedBarrelPrefab;

        [Header("Customizable Options")]
        [SerializeField] private float minTime = 0.05f;
        [SerializeField] private float maxTime = 0.25f;

        [Header("Explosion Options")]
        [SerializeField] private float explosionForce = 4000.0f;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip equipClip;
        [SerializeField] private float equipVolume = 1f;
        [SerializeField] private AudioClip placementClip;
        [SerializeField] private float placementVolume = 1f;
        [SerializeField] private AudioClip explosionClip;
        [SerializeField] private float explosionVolume = 1f;

        #endregion

        #region FIELDS

        private bool shouldExplode = false;
        private bool routineStarted = false;
        private IAudioManagerService audioService;

        #endregion

        #region PROPERTIES

        public bool IsExploding => shouldExplode;

        #endregion

        #region EVENTS

        #endregion

        #region CONSTANTS

        #endregion

        #region UNITY

        private void Awake() {
            audioService = ServiceLocator.Current.Get<IAudioManagerService>();
        }

        private void Update() {
            if (shouldExplode && !routineStarted) {
                StartCoroutine(Explode());
                routineStarted = true;
            }
        }

        #endregion

        #region METHODS

        #region ITEM BEHAVIOUR IMPLEMENTATION

        public override string GetItemID() {
            if (barrelData == null) {
                Debug.LogWarning("[ExplosiveBarrel] barrelData is null!", gameObject);
                return "explosive_barrel_null";
            }
            return barrelData.ItemID;
        }

        public override string GetDisplayName() {
            if (barrelData == null) return "Unknown";
            return barrelData.ItemName;
        }

        public override Sprite GetIcon() {
            if (hudIcon == null) {
                Debug.LogWarning("[ExplosiveBarrel] hudIcon is null!", gameObject);
                return null;
            }
            return hudIcon;
        }

        /// <summary>
        /// Called when player selects this item (key 7).
        /// Starts placement mode (ghost preview appears).
        /// </summary>
        public override void OnSelected() {
            PlayEquipSound();
            if (PlayerProgress.Instance != null) {
                string id = GetItemID();
                int total = PlayerProgress.Instance.GetItemTotal(id);
                PlayerProgress.Instance.SetItemCurrent(id, total > 0 ? 1 : 0);
            }
            if (BuildingController.Instance != null && barrelData != null) {
                BuildingController.Instance.StartPlacement(barrelData);
            }
        }

        /// <summary>
        /// Called when player selects another item. Cancels placement mode.
        /// </summary>
        public override void OnDeselected() {
            if (BuildingController.Instance != null && BuildingController.Instance.IsPlacing) {
                BuildingController.Instance.CancelPlacement();
            }
        }

        /// <summary>
        /// Normal use: Place explosive barrel with normal explosion force.
        /// </summary>
        public override void OnUse() {
            if (!CanBeUsed()) {
                return;
            }
        }

        /// <summary>
        /// Checks if barrel is unlocked and has quantity in inventory.
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
        /// Explosive barrel does not need a weapon pose. Keeps hands lowered when equipped.
        /// </summary>
        public override bool KeepHolsteredOnEquip() => true;

        #endregion

        #region IDAMAGEABLE IMPLEMENTATION

        /// <summary>
        /// IDamageable interface method.
        /// When barrel takes damage, triggers explosion.
        /// </summary>
        public void TakeDamage(float amount) {
            shouldExplode = true;
        }

        #endregion

        #region GIZMOS

        private void OnDrawGizmosSelected() {
            float radius = barrelData != null ? barrelData.ExplosionRadius : 5f;
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, radius);
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }

        #endregion

        #region EXPLOSION LOGIC

        /// <summary>
        /// Coroutine that handles explosion with random delay.
        /// Applies physics force, triggers chain reactions, damages enemies, and spawns VFX.
        /// </summary>
        private IEnumerator Explode() {
            float randomDelay = Random.Range(minTime, maxTime);
            yield return new WaitForSeconds(randomDelay);

            PlayExplosionSound();

            SlowMotionManager.Instance?.TriggerSlowMotion(1.0f);

            if (destroyedBarrelPrefab != null) {
                Instantiate(destroyedBarrelPrefab, transform.position, transform.rotation);
            }

            float radius = barrelData.ExplosionRadius;
            float damage = barrelData.Damage;
            if (PlayerProgress.Instance != null) {
                int level = PlayerProgress.Instance.GetItemLevel(GetItemID());
                radius = barrelData.GetRadiusAtLevel(level);
                damage = barrelData.GetDamageAtLevel(level);
            }

            Vector3 explosionPos = transform.position;
            Collider[] colliders = Physics.OverlapSphere(explosionPos, radius);

            foreach (Collider hit in colliders) {
                Rigidbody rb = hit.GetComponent<Rigidbody>();
                if (rb != null) {
                    rb.AddExplosionForce(explosionForce * 50, explosionPos, radius);
                }

                if (hit.transform.tag == "ExplosiveBarrel") {
                    ExplosiveBarrel otherBarrel = hit.GetComponent<ExplosiveBarrel>();
                    if (otherBarrel != null) {
                        otherBarrel.shouldExplode = true;
                    }
                }

                if (barrelData == null) continue;
                EnemyBase enemy = hit.GetComponent<EnemyBase>();
                if (enemy != null) {
                    enemy.TakeDamage(damage);
                }
            }

            RaycastHit hitInfo;
            if (Physics.Raycast(transform.position, Vector3.down, out hitInfo, 50f)) {
                if (explosionPrefab != null) {
                    Instantiate(explosionPrefab, hitInfo.point,
                        Quaternion.FromToRotation(Vector3.forward, hitInfo.normal));
                }
            }

            Destroy(gameObject);
        }

        #endregion

        #region AUDIO

        public void PlayEquipSound() {
            if (equipClip != null && audioService != null) {
                audioService.PlaySFX2D(equipClip, equipVolume);
            }
        }

        public void PlayPlacementSound() {
            if (placementClip != null && audioService != null) {
                audioService.PlaySFX2D(placementClip, placementVolume);
            }
        }

        private void PlayExplosionSound() {
            if (explosionClip != null && audioService != null) {
                audioService.PlaySFX3D(explosionClip, transform.position, explosionVolume);
            }
        }

        #endregion

        #region DEBUG

        #endregion

        #endregion
    }
}
