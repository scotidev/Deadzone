using UnityEngine;
using System.Collections;
using InfimaGames.LowPolyShooterPack;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Thrown grenade behavior. Attached to the prefab that is instantiated when player releases fire.
    /// Handles detonation timer, explosion physics, damage application, and VFX/audio.
    /// Each thrown grenade is independent and manages its own lifecycle.
    /// </summary>
    public class GrenadeThrown : MonoBehaviour {

        #region SERIALIZED FIELDS

        [SerializeField] private GrenadeDataSO grenadeData;
        
        [Header("Detonation")]
        [SerializeField] private float fuseTime = 3f;

        [Header("Explosion VFX")]
        [SerializeField] private Transform explosionVFXPrefab;
        [SerializeField] private float minExplosionDelay = 0.05f;
        [SerializeField] private float maxExplosionDelay = 0.25f;

        [Header("Audio")]
        [SerializeField] private AudioClip explosionClip;
        [SerializeField] private float explosionVolume = 1f;

        #endregion

        #region FIELDS

        private Coroutine detonationCoroutine;
        private IAudioManagerService audioService;

        #endregion

        #region UNITY LIFECYCLE

        private void Awake() {
            audioService = ServiceLocator.Current.Get<IAudioManagerService>();
            Debug.Log($"[GrenadeThrown] Awake: audioService obtained: {(audioService != null ? "SUCCESS ✓" : "NULL ✗")}");
        }

        private void Start() {
            // CONCEITO: Iniciar a corrotina de detonação assim que a granada é lançada.
            // Cada instância gerencia seu próprio timer de explosão.
            detonationCoroutine = StartCoroutine(DetonationTimer());
        }

        #endregion

        #region DETONATION LOGIC

        /// <summary>
        /// Coroutine to wait for fuse time and trigger explosion.
        /// CONCEITO: IEnumerator permite pausar a execução com "yield return".
        /// A corrotina espera pelo fuse time e depois chama Explode().
        /// </summary>
        private IEnumerator DetonationTimer() {
            // CONCEITO: Delay aleatório pequeno torna a explosão mais realista.
            // Grenadas não explodem instantaneamente quando batem no chão.
            float randomDelay = Random.Range(minExplosionDelay, maxExplosionDelay);
            yield return new WaitForSeconds(randomDelay);

            // CONCEITO: Esperar pelo fuse time (3 segundos por padrão).
            // Durante este tempo, a granada já está em voo.
            yield return new WaitForSeconds(fuseTime);

            // CONCEITO: Chegou a hora de explodir. Executar lógica de explosão.
            yield return StartCoroutine(Explode());
        }

        /// <summary>
        /// Explode grenade at its current position.
        /// Apply damage to enemies, physics force to rigidbodies, and spawn VFX.
        /// CONCEITO: Este método faz toda a lógica de explosão (dano, física, efeitos).
        /// </summary>
        private IEnumerator Explode() {
            if (grenadeData == null) {
                Debug.LogWarning("[GrenadeThrown] GrenadeDataSO is null during explosion!", gameObject);
                Destroy(gameObject);
                yield break;
            }

            Vector3 explosionPos = transform.position;

            // CONCEITO: Tocar som de explosão 3D na posição da granada.
            // Som 3D significa que a fonte é posicionada no mundo, afetando volume e pan por distância.
            PlayExplosionSound(explosionPos);

            // CONCEITO: Obter o nível da granada para escalar dano e raio.
            // Level 1 = base, Level 2 = base * 1.1, Level 3 = base * 1.2, etc.
            int grenadeLevel = 1;
            if (PlayerProgress.Instance != null) {
                grenadeLevel = PlayerProgress.Instance.GetItemLevel(grenadeData.ItemID);
            }

            // CONCEITO: GetDamageAtLevel e GetRadiusAtLevel aplicam scaling automaticamente.
            // Isso permite que grenadas de níveis maiores façam mais dano e tenham raio maior.
            float damage = grenadeData.GetDamageAtLevel(grenadeLevel);
            float radius = grenadeData.GetRadiusAtLevel(grenadeLevel);

            // CONCEITO: Physics.OverlapSphere encontra todos os colisores
            // dentro de uma esfera de raio 'radius' centrada em 'explosionPos'.
            // Isso nos dá todos os objetos atingidos pela explosão.
            Collider[] colliders = Physics.OverlapSphere(explosionPos, radius);

            foreach (Collider hit in colliders) {
                // CONCEITO: Se o collider tiver Rigidbody, aplicar força de explosão.
                // AddExplosionForce simula uma onda de choque radial realista.
                Rigidbody rb = hit.GetComponent<Rigidbody>();
                if (rb != null) {
                    rb.AddExplosionForce(5000f, explosionPos, radius);
                }

                // CONCEITO: Se for um inimigo, aplicar dano direto.
                // Cada inimigo deve ter componente com tag "Enemy" ou interface IDamageable.
                if (hit.CompareTag("Enemy")) {
                    EnemyBase enemy = hit.GetComponent<EnemyBase>();
                    if (enemy != null) {
                        enemy.TakeDamage(damage);
                        Debug.Log($"[GrenadeThrown] Enemy hit for {damage} damage at level {grenadeLevel}");
                    }
                }

                // Chain reaction: trigger other explosive barrels
                // CONCEITO: Se a explosão acertar um barril explosivo,
                // aquele barril também explodirá (reação em cadeia).
                if (hit.transform.CompareTag("ExplosiveBarrel")) {
                    ExplosiveBarrel otherBarrel = hit.GetComponent<ExplosiveBarrel>();
                    if (otherBarrel != null) {
                        otherBarrel.TakeDamage(0);
                    }
                }
            }

            // CONCEITO: Raycast para baixo para encontrar o chão onde colocar o VFX.
            // A maioria das explosões acontece perto do solo, então colocar efeito lá é visual.
            RaycastHit hitInfo;
            if (Physics.Raycast(explosionPos, Vector3.down, out hitInfo, 50f)) {
                if (explosionVFXPrefab != null) {
                    // CONCEITO: Instantiar VFX no ponto onde o raycast acertou o chão.
                    // Rotacionar para que fique alinhado com a normal da superfície.
                    Instantiate(explosionVFXPrefab, hitInfo.point,
                        Quaternion.FromToRotation(Vector3.forward, hitInfo.normal));
                    Debug.Log($"[GrenadeThrown] VFX spawned at ground impact point");
                }
            }

            // CONCEITO: Destruir o GameObject da granada lançada.
            // Ela já explodiu, não precisa mais existir.
            // OnDestroy será chamado automaticamente.
            Destroy(gameObject);
        }

        #endregion

        #region AUDIO

        /// <summary>
        /// Play 3D explosion sound at explosion position.
        /// CONCEITO: PlaySFX3D faz o som ouvido em 3D, com volume afetado pela distância do listener.
        /// </summary>
        private void PlayExplosionSound(Vector3 position) {
            // Re-cache audioService if null to avoid MissingReferenceException.
            if (audioService == null) {
                audioService = ServiceLocator.Current.Get<IAudioManagerService>();
            }

            if (explosionClip != null && audioService != null) {
                audioService.PlaySFX3D(explosionClip, position, explosionVolume);
            }
        }

        #endregion

    }

}
