using UnityEngine;
using System.Collections;
using InfimaGames.LowPolyShooterPack;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Thrown grenade behavior. Attached to the prefab instantiated when player releases fire.
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

        #region PROPERTIES

        #endregion

        #region EVENTS

        #endregion

        #region CONSTANTS

        #endregion

        #region UNITY

        private void Awake() {
            audioService = ServiceLocator.Current.Get<IAudioManagerService>();
        }

        private void Start() {
            detonationCoroutine = StartCoroutine(DetonationTimer());
        }

        #endregion

        #region METHODS

        #region DETONATION LOGIC

        /// <summary>
        /// Coroutine that waits for fuse time and triggers explosion.
        /// </summary>
        private IEnumerator DetonationTimer() {
            float randomDelay = Random.Range(minExplosionDelay, maxExplosionDelay);
            yield return new WaitForSeconds(randomDelay);

            yield return new WaitForSeconds(fuseTime);

            yield return StartCoroutine(Explode());
        }

        /// <summary>
        /// Explodes the grenade at its current position.
        /// Applies damage to enemies, physics force to rigidbodies, and spawns VFX.
        /// </summary>
        private IEnumerator Explode() {
            if (grenadeData == null) {
                Debug.LogWarning("[GrenadeThrown] GrenadeDataSO is null during explosion!", gameObject);
                Destroy(gameObject);
                yield break;
            }

            Vector3 explosionPos = transform.position;

            PlayExplosionSound(explosionPos);

            int grenadeLevel = 1;
            if (PlayerProgress.Instance != null) {
                grenadeLevel = PlayerProgress.Instance.GetItemLevel(grenadeData.ItemID);
            }

            float damage = grenadeData.GetDamageAtLevel(grenadeLevel);
            float radius = grenadeData.GetRadiusAtLevel(grenadeLevel);

            Collider[] colliders = Physics.OverlapSphere(explosionPos, radius);

            foreach (Collider hit in colliders) {
                Rigidbody rb = hit.GetComponent<Rigidbody>();
                if (rb != null) {
                    rb.AddExplosionForce(5000f, explosionPos, radius);
                }

                if (hit.CompareTag("Enemy")) {
                    EnemyBase enemy = hit.GetComponent<EnemyBase>();
                    if (enemy != null) {
                        enemy.TakeDamage(damage);
                    }
                }

                if (hit.transform.CompareTag("ExplosiveBarrel")) {
                    ExplosiveBarrel otherBarrel = hit.GetComponent<ExplosiveBarrel>();
                    if (otherBarrel != null) {
                        otherBarrel.TakeDamage(0);
                    }
                }
            }

            RaycastHit hitInfo;
            if (Physics.Raycast(explosionPos, Vector3.down, out hitInfo, 50f)) {
                if (explosionVFXPrefab != null) {
                    Instantiate(explosionVFXPrefab, hitInfo.point,
                        Quaternion.FromToRotation(Vector3.forward, hitInfo.normal));
                }
            }

            Destroy(gameObject);
        }

        #endregion

        #region AUDIO

        /// <summary>
        /// Plays 3D explosion sound at the explosion position.
        /// </summary>
        private void PlayExplosionSound(Vector3 position) {
            if (audioService == null) {
                audioService = ServiceLocator.Current.Get<IAudioManagerService>();
            }

            if (explosionClip != null && audioService != null) {
                audioService.PlaySFX3D(explosionClip, position, explosionVolume);
            }
        }

        #endregion

        #region DEBUG

        #endregion

        #endregion

    }

}
