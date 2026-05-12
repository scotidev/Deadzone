using UnityEngine;
using InfimaGames.LowPolyShooterPack;
using Deadzone.Interfaces;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Grenade consumable item. Throws a grenade when used.
    /// </summary>
    public class Grenade : ItemBehaviour {

        #region SERIALIZED FIELDS

        [SerializeField] private GrenadeDataSO grenadeData;
        [SerializeField] private Sprite hudIcon;
        [SerializeField] private GameObject grenadePrefab;
        [SerializeField] private float throwForce = 20f;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip equipClip;
        [SerializeField] private float equipVolume = 1f;
        [SerializeField] private AudioClip pinPullClip;
        [SerializeField] private float pinPullVolume = 1f;
        [SerializeField] private AudioClip throwClip;
        [SerializeField] private float throwVolume = 1f;

        #endregion

        #region FIELDS

        private IAudioManagerService audioService;

        #endregion

        #region UNITY

        private void Awake() {
            audioService = ServiceLocator.Current.Get<IAudioManagerService>();
        }

        #endregion

        #region ITEM BEHAVIOUR IMPLEMENTATION

        public override string GetItemID() {
            if (grenadeData == null) {
                Debug.LogWarning("[Grenade] grenadeData is null!", gameObject);
                return "grenade_null";
            }
            return grenadeData.ItemID;
        }

        public override string GetDisplayName() {
            if (grenadeData == null) return "Unknown";
            return grenadeData.ItemName;
        }

        public override Sprite GetIcon() {
            if (hudIcon == null) {
                Debug.LogWarning("[Grenade] hudIcon is null!", gameObject);
                return null;
            }
            return hudIcon;
        }

        /// <summary>
        /// Called when player selects this item (key 5).
        /// Activate visual representation (grenade model in hand).
        /// </summary>
        public override void OnSelected() {
            PlayEquipSound();
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Called when player selects another item.
        /// Deactivate visual.
        /// </summary>
        public override void OnDeselected() {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// NORMAL use: Throw grenade with normal damage.
        /// CONCEITO: Instancia a granada prefab na posição da câmera do jogador
        /// e aplica velocidade inicial (throwForce) na direção da câmera.
        /// </summary>
        public override void OnUse() {
            if (!CanBeUsed() || grenadeData == null) {
                return;
            }

            ThrowGrenade(grenadeData.damage);
        }

        /// <summary>
        /// Helper method to throw grenade with specified damage.
        /// Gets camera position and direction from Character.
        /// </summary>
        private void ThrowGrenade(float damage) {
            if (grenadePrefab == null) {
                Debug.LogWarning("[Grenade] grenadePrefab is null!", gameObject);
                return;
            }

            PlayPinPullSound();

            Character character = GetComponentInParent<Character>();
            if (character == null) {
                return;
            }

            Transform cameraTransform = character.GetCameraWorld().transform;
            if (cameraTransform == null) {
                return;
            }

            // Instantiate grenade at camera position
            GameObject grenade = Instantiate(grenadePrefab, cameraTransform.position, Quaternion.identity);

            // Apply throw force
            Rigidbody rb = grenade.GetComponent<Rigidbody>();
            if (rb != null) {
                rb.linearVelocity = cameraTransform.forward * throwForce;
            }

            PlayThrowSound();

            // Consume 1 grenade from inventory
            if (PlayerProgress.Instance != null) {
                PlayerProgress.Instance.ConsumeItem(GetItemID(), 1);
            }
        }

        /// <summary>
        /// Check if grenade is unlocked (for selection). Quantity check happens in OnUse().
        /// </summary>
        public override bool CanBeUsed() {
            if (PlayerProgress.Instance == null) {
                Debug.LogWarning($"[Grenade] CanBeUsed: PlayerProgress.Instance is NULL!");
                return false;
            }

            // CONCEITO: CanBeUsed() é para seleção, apenas verifica se desbloqueado.
            // A checagem de quantidade é feita em OnUse(), não em CanBeUsed().
            // Isso permite selecionar qualquer item desbloqueado, mesmo sem quantidade.
            bool isUnlocked = PlayerProgress.Instance.IsItemUnlocked(GetItemID());
            Debug.Log($"[Grenade] CanBeUsed check: ID={GetItemID()}, Unlocked={isUnlocked}");
            return isUnlocked;
        }

        #endregion

        #region AUDIO

        private void PlayEquipSound() {
            if (equipClip != null && audioService != null) {
                audioService.PlaySFX2D(equipClip, equipVolume);
            }
        }

        private void PlayPinPullSound() {
            if (pinPullClip != null && audioService != null) {
                audioService.PlaySFX2D(pinPullClip, pinPullVolume);
            }
        }

        private void PlayThrowSound() {
            if (throwClip != null && audioService != null) {
                audioService.PlaySFX2D(throwClip, throwVolume);
            }
        }

        #endregion
    }
}
