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
        /// Start placement mode (ghost preview appears).
        /// </summary>
        public override void OnSelected() {
            PlayEquipSound();
            // FIXED: Set current ammo to 1 when buildable is selected (1 in hand ready to place).
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
        /// Called when player selects another item.
        /// Cancel placement mode.
        /// </summary>
        public override void OnDeselected() {
            if (BuildingController.Instance != null && BuildingController.Instance.IsPlacing) {
                BuildingController.Instance.CancelPlacement();
            }
        }

        /// <summary>
        /// NORMAL use: Place explosive barrel with normal explosion force.
        /// </summary>
        public override void OnUse() {
            if (!CanBeUsed()) {
                return;
            }
        }

        /// <summary>
        /// Check if barrel is unlocked AND has quantity in inventory.
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
        /// ExplosiveBarrel nao precisa de pose de arma. Mantem maos abaixadas ao equipar.
        /// </summary>
        public override bool KeepHolsteredOnEquip() => true;

        #endregion

        #region UNITY LIFECYCLE

        private void Awake() {
            audioService = ServiceLocator.Current.Get<IAudioManagerService>();
        }

        private void Update() {
            // CONCEITO: Verificar a cada frame se o barril deve explodir.
            // Update() é o método chamado uma vez por frame no Unity.
            // Aqui fazemos uma simples verificação (shouldExplode == true).
            if (shouldExplode && !routineStarted) {
                // Iniciar a corrotina de explosão
                // CONCEITO: Corrotinas permitem esperar tempo real no código.
                // Sem corrotinas, teríamos que usar um timer manual.
                StartCoroutine(Explode());
                routineStarted = true;
            }
        }

        #endregion

        #region IDAMAGEABLE IMPLEMENTATION

        /// <summary>
        /// IDamageable interface method.
        /// When barrel takes damage, trigger explosion.
        /// </summary>
        public void TakeDamage(float amount) {
            // Trigger explosion when hit
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
        /// Corrotine to handle explosion with delay.
        /// CONCEITO: IEnumerator permite pausar e retomar a execução.
        /// "yield return new WaitForSeconds(time)" pausa a corrotina por 'time' segundos.
        /// Isso simula o delay antes da explosão.
        /// </summary>
        private IEnumerator Explode() {
            // CONCEITO: Delay aleatório torna a explosão mais realista.
            // Barris não explodem instantaneamente, levam um tempo pequeno.
            float randomDelay = Random.Range(minTime, maxTime);
            yield return new WaitForSeconds(randomDelay);

            PlayExplosionSound();

            // Trigger slow-motion effect
            // CONCEITO: O operador "?." (null-conditional operator) chama o método
            // SOMENTE se SlowMotionManager.Instance não for null.
            // Isso evita exceções se o manager não existir na cena.
            SlowMotionManager.Instance?.TriggerSlowMotion(1.0f);

            // Instantiate destroyed barrel prefab
            if (destroyedBarrelPrefab != null) {
                Instantiate(destroyedBarrelPrefab, transform.position, transform.rotation);
            }

            // Get scaled radius and damage from BuildableDataSO
            // CONCEITO: O raio e dano escalam com o nível de upgrade do item.
            float radius = barrelData.ExplosionRadius;
            float damage = barrelData.Damage;
            if (PlayerProgress.Instance != null) {
                int level = PlayerProgress.Instance.GetItemLevel(GetItemID());
                radius = barrelData.GetRadiusAtLevel(level);
                damage = barrelData.GetDamageAtLevel(level);
            }

            // Apply explosion physics
            // CONCEITO: Physics.OverlapSphere encontra todos os colisores
            // dentro de uma esfera de raio 'radius' centrada em 'explosionPos'.
            // Isso nos dá todos os objetos atingidos pela explosão.
            Vector3 explosionPos = transform.position;
            Collider[] colliders = Physics.OverlapSphere(explosionPos, radius);

            foreach (Collider hit in colliders) {
                // Apply explosion force to rigidbodies
                Rigidbody rb = hit.GetComponent<Rigidbody>();
                if (rb != null) {
                    // CONCEITO: AddExplosionForce aplica uma força radial
                    // que simula uma explosão física realista.
                    rb.AddExplosionForce(explosionForce * 50, explosionPos, radius);
                }

                // Chain reaction: trigger other explosive barrels
                // CONCEITO: Se a explosão acertar outro barril explosivo,
                // aquele barril também explodirá (reação em cadeia).
                if (hit.transform.tag == "ExplosiveBarrel") {
                    ExplosiveBarrel otherBarrel = hit.GetComponent<ExplosiveBarrel>();
                    if (otherBarrel != null) {
                        otherBarrel.shouldExplode = true;
                    }
                }

                // Damage enemies in radius, scaled by upgrade level
                // CONCEITO: O dano vem do BuildableDataSO (única fonte de verdade).
                // GetDamageAtLevel(level) já aplica o damageScaling configurado no SO.
                if (barrelData == null) continue;
                EnemyBase enemy = hit.GetComponent<EnemyBase>();
                if (enemy != null) {
                    enemy.TakeDamage(damage);
                }
            }

            // Raycast downwards to find ground and spawn explosion effect
            // CONCEITO: Raycasting dispara uma "linha invisível" e retorna
            // o primeiro objeto que acertou. Aqui estamos procurando o ground.
            RaycastHit hitInfo;
            if (Physics.Raycast(transform.position, Vector3.down, out hitInfo, 50f)) {
                if (explosionPrefab != null) {
                    Instantiate(explosionPrefab, hitInfo.point,
                        Quaternion.FromToRotation(Vector3.forward, hitInfo.normal));
                }
            }

            // Destroy the barrel gameobject
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
    }
}
