using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Implementation of a simple melee weapon. This is not a "weapon" in the traditional sense, as it does not shoot projectiles, but it is still a "weapon" in terms of gameplay mechanics. It performs a short-range attack that damages enemies in front of the player.
    /// </summary>
    public class MeleeWeapon : MonoBehaviour {

        #region SERIALIZED FIELDS

        [Header("Melee")]

        [SerializeField] private float meleeDamage = 30.0f;
        [SerializeField] private float meleeRange = 1.4f;
        [SerializeField] private Vector3 meleeHalfExtents = new Vector3(0.22f, 0.22f, 0.7f);
        [SerializeField] private float meleeCooldown = 0.5f;
        [SerializeField] private float meleeVisualDuration = 0.3f;
        [SerializeField] private float meleeAttackDuration = 0.5f;

        [SerializeField] private Vector3 visualLocalPosition = new Vector3(0.18f, -0.2f, 0.7f);

        [SerializeField] private Vector3 visualLocalEuler = new Vector3(0.0f, 0.0f, -20.0f);

        [SerializeField] private Vector3 visualLocalScale = new Vector3(0.08f, 0.16f, 0.8f);

        [Header("Audio")]
        [SerializeField] private AudioClip meleeSFX;
        [SerializeField] private float meleeSFXVolume = 1f;

        #endregion

        #region FIELDS

        private Character playerCharacter;
        private CapsuleCollider playerCapsule;
        private float lastMeleeTime = -10.0f;
        private GameObject meleeVisual;
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
            SetupMeleeVisual();
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

        private void TryMeleeAttack() {
            if (Time.time - lastMeleeTime < meleeCooldown)
                return;

            if (playerCharacter == null)
                return;

            if (playerCharacter.IsAttackingMelee())
                return;

            lastMeleeTime = Time.time;
            isAttacking = true;

            playerCharacter.StartMeleeAttack();

            StartCoroutine(MeleeAttackRoutine());
        }

        private IEnumerator MeleeAttackRoutine() {
            // CONCEITO: Resetar a flag de hitmarker no início de cada novo ataque.
            // Isso garante que o hitmarker seja disparado no máximo uma vez por ataque.
            hitmarkerTriggeredThisAttack = false;

            if (meleeVisual != null)
                meleeVisual.SetActive(true);

            // Play melee SFX when attack starts.
            if (meleeSFX != null && audioService != null) {
                audioService.PlaySFX2D(meleeSFX, meleeSFXVolume);
            }

            PerformMeleeDamage();

            yield return new WaitForSeconds(meleeAttackDuration);

            if (meleeVisual != null)
                meleeVisual.SetActive(false);

            if (playerCharacter != null) {
                playerCharacter.EndMeleeAttack();
            }

            isAttacking = false;
        }

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

                // CONCEITO: Disparar o hitmarker apenas uma vez por ataque.
                // A flag 'hitmarkerTriggeredThisAttack' garante que mesmo com múltiplos inimigos atingidos,
                // o feedback visual/audio do hitmarker ocorra apenas uma vez para melhor UX.
                if (!hitmarkerTriggeredThisAttack) {
                    HitmarkerManager.TriggerHitmarker();
                    hitmarkerTriggeredThisAttack = true;
                }

                Debug.Log($"[MELEE] Acertou inimigo: {enemy.name}");
                Debug.DrawLine(cameraWorld.transform.position, enemy.transform.position, Color.red, 0.25f);
            }

            for (int i = 0; i < hits; i++)
                meleeHits[i] = null;
        }

        private IEnumerator ShowMeleeVisualRoutine() {
            if (meleeVisual != null)
                meleeVisual.SetActive(true);

            yield return new WaitForSeconds(meleeVisualDuration);

            if (meleeVisual != null)
                meleeVisual.SetActive(false);
        }

        private void SetupMeleeVisual() {
            if (playerCharacter == null)
                return;

            Camera cameraWorld = playerCharacter.GetCameraWorld();
            if (cameraWorld == null)
                return;

            meleeVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            meleeVisual.name = "MeleeRect";
            meleeVisual.transform.SetParent(cameraWorld.transform, false);
            meleeVisual.transform.localPosition = visualLocalPosition;
            meleeVisual.transform.localRotation = Quaternion.Euler(visualLocalEuler);
            meleeVisual.transform.localScale = visualLocalScale;

            Collider visualCollider = meleeVisual.GetComponent<Collider>();
            if (visualCollider != null)
                visualCollider.enabled = false;

            meleeVisual.SetActive(false);
        }

        #endregion
    }
}