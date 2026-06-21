using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Implementation of a simple melee weapon. Performs a short-range attack that damages enemies
    /// in front of the player using an OverlapBox check. Not a projectile-based weapon.
    /// </summary>
    public class MeleeWeapon : MonoBehaviour {

        #region SERIALIZED FIELDS

        [Header("Melee")]

        [SerializeField] private float meleeDamage = 30.0f;
        [SerializeField] private float meleeRange = 1.4f;
        [SerializeField] private Vector3 meleeHalfExtents = new Vector3(0.22f, 0.22f, 0.7f);
        [SerializeField] private float meleeCooldown = 0.5f;
        [SerializeField] private float meleeAttackDuration = 0.5f;

        [Header("Melee Visual")]
        [Tooltip("GameObject da faca (filha de Inventory) que será ativada/desativada no ataque melee.")]
        [SerializeField] private GameObject meleeKnifeVisual;

        [Header("Audio")]
        [SerializeField] private AudioClip meleeSFX;
        [SerializeField] private float meleeSFXVolume = 1f;

        [Header("Tutorial Settings")]
        [Tooltip("Referência ao Mesh dos braços do jogador para ativar durante o soco no tutorial.")]
        [SerializeField] private GameObject playerArmsMesh;

        #endregion

        #region FIELDS

        private Character playerCharacter;
        private CapsuleCollider playerCapsule;
        private float lastMeleeTime = -10.0f;
        private readonly Collider[] meleeHits = new Collider[16];
        private bool isAttacking;
        private bool hitmarkerTriggeredThisAttack;
        private IAudioManagerService audioService;

        #endregion

        #region UNITY

        private void Awake() {
            playerCharacter = ServiceLocator.Current.Get<IGameModeService>().GetPlayerCharacter() as Character;
            if (playerCharacter != null) {
                playerCapsule = playerCharacter.GetComponent<CapsuleCollider>();
            }

            audioService = ServiceLocator.Current.Get<IAudioManagerService>();
        }

        private void Start() {
            if (meleeKnifeVisual != null)
                meleeKnifeVisual.SetActive(false);
        }

        private void Update() {
            if (Keyboard.current == null)
                return;

            if (playerCharacter == null)
                return;

            if (!playerCharacter.IsCursorLocked() || playerCharacter.IsInterfaceMode())
                return;

            if (isAttacking)
                return;

            if (Keyboard.current.fKey.wasPressedThisFrame)
                TryMeleeAttack();
        }

        #endregion
        #region METHODS

        /// <summary>
        /// Attempts to start a melee attack if cooldown and character state permit.
        /// </summary>
        private void TryMeleeAttack() {
            if (Time.time - lastMeleeTime < meleeCooldown)
                return;

            if (playerCharacter == null)
                return;

            if (!playerCharacter.StartMeleeAttack())
                return;

            lastMeleeTime = Time.time;
            isAttacking = true;

            StartCoroutine(MeleeAttackRoutine());
        }

        /// <summary>
        /// Coroutine that plays the melee attack over its duration, including visuals and damage.
        /// </summary>
        private IEnumerator MeleeAttackRoutine() {
            if (playerArmsMesh != null) {
                playerArmsMesh.SetActive(true);
            }

            hitmarkerTriggeredThisAttack = false;

            if (meleeKnifeVisual != null)
                meleeKnifeVisual.SetActive(true);

            if (meleeSFX != null && audioService != null) {
                audioService.PlaySFX2D(meleeSFX, meleeSFXVolume);
            }

            PerformMeleeDamage();

            yield return new WaitForSeconds(meleeAttackDuration);

            if (meleeKnifeVisual != null)
                meleeKnifeVisual.SetActive(false);

            if (playerCharacter != null) {
                playerCharacter.EndMeleeAttack();
            }

            if (PlayerProgress.Instance != null && !PlayerProgress.Instance.IsWeaponUnlocked("1")) {
                if (playerArmsMesh != null) {
                    playerArmsMesh.SetActive(false);
                }
            }

            isAttacking = false;
        }

        /// <summary>
        /// Performs the melee damage check using an OverlapBox in front of the camera.
        /// </summary>
        private void PerformMeleeDamage() {
            if (playerCharacter == null)
                return;

            Camera cameraWorld = playerCharacter.GetCameraWorld();
            if (cameraWorld == null)
                return;

            Vector3 center = cameraWorld.transform.position + cameraWorld.transform.forward * meleeRange;
            Quaternion orientation = cameraWorld.transform.rotation;

            int hits = Physics.OverlapBoxNonAlloc(
                center,
                meleeHalfExtents,
                meleeHits,
                orientation,
                ~0,
                QueryTriggerInteraction.Ignore
            );

            var damagedEnemies = new HashSet<EnemyBase>();

            for (int i = 0; i < hits; i++) {
                Collider hitCollider = meleeHits[i];

                if (hitCollider == null || hitCollider == playerCapsule)
                    continue;

                EnemyBase enemy = hitCollider.GetComponentInParent<EnemyBase>();
                if (enemy == null)
                    continue;

                if (!damagedEnemies.Add(enemy))
                    continue;

                enemy.TakeDamage(meleeDamage);

                if (!hitmarkerTriggeredThisAttack) {
                    HitmarkerManager.TriggerHitmarker();
                    hitmarkerTriggeredThisAttack = true;
                }

                Debug.DrawLine(cameraWorld.transform.position, enemy.transform.position, Color.red, 0.25f);
            }

            for (int i = 0; i < hits; i++)
                meleeHits[i] = null;
        }

        #endregion
    }
}
