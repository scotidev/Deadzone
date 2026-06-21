using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using InfimaGames.LowPolyShooterPack;
using Deadzone.Interfaces;
using Deadzone.UI;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Grenade consumable item. Players hold fire button to arm, release to throw.
    /// Hold-to-charge mechanic with detonation and explosion damage.
    /// </summary>
    public class Grenade : ItemBehaviour {

        #region ENUMS

        private enum GrenadeState {
            Idle,
            Pinned,
            Thrown,
            Exploded
        }

        #endregion

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
        private GrenadeState currentState = GrenadeState.Idle;
        private GameObject thrownGrenadeInstance;
        private InputAction fireAction;

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

        private void OnDisable() {
            UnsubscribeFromFireInput();
        }

        private void OnDestroy() {
            UnsubscribeFromFireInput();
        }

        #endregion

        #region METHODS

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
        /// Activates visual representation and subscribes to Fire input callbacks.
        /// </summary>
        public override void OnSelected() {
            string id = GetItemID();
            int total = PlayerProgress.Instance != null ? PlayerProgress.Instance.GetItemTotal(id) : -1;

            if (PlayerProgress.Instance != null) {
                if (total <= 0) {
                    return;
                }
                PlayerProgress.Instance.SetItemCurrent(id, 1);
            }

            PlayEquipSound();
            gameObject.SetActive(true);
            SubscribeToFireInput();
            currentState = GrenadeState.Idle;
        }

        /// <summary>
        /// Called when player selects another item.
        /// Deactivates visual and unsubscribes from input.
        /// </summary>
        public override void OnDeselected() {
            if (currentState == GrenadeState.Pinned) {
                UnsubscribeFromFireInput();
                currentState = GrenadeState.Idle;
            } else if (currentState == GrenadeState.Thrown && thrownGrenadeInstance != null) {
            }

            gameObject.SetActive(false);
            UnsubscribeFromFireInput();
        }

        /// <summary>
        /// Normal use: Not used directly. Grenade uses input callbacks instead.
        /// </summary>
        public override void OnUse() {
        }

        /// <summary>
        /// Checks if grenade can be selected (unlocked and has quantity available).
        /// </summary>
        public override bool CanBeUsed() {
            if (PlayerProgress.Instance == null) {
                return false;
            }

            bool isUnlocked = PlayerProgress.Instance.IsItemUnlocked(GetItemID());
            int totalAmmo = PlayerProgress.Instance.GetItemTotal(GetItemID());
            if (isUnlocked && totalAmmo <= 0)
                FeedbackMessageUI.Instance?.Show();
            return isUnlocked && totalAmmo > 0;
        }

        #endregion

        #region ANIMATION

        /// <summary>
        /// Grenade does not need a weapon pose. Keeps hands lowered when equipped.
        /// </summary>
        public override bool KeepHolsteredOnEquip() => true;

        #endregion

        #region INPUT HANDLING

        /// <summary>
        /// Subscribes to Fire input using InputSystem callbacks.
        /// Allows the grenade to control its own hold/release logic.
        /// </summary>
        private void SubscribeToFireInput() {
            if (fireAction != null) return;

            PlayerInput playerInput = GetComponentInParent<PlayerInput>();
            if (playerInput == null) {
                Debug.LogWarning("[Grenade] PlayerInput not found in parent hierarchy!");
                return;
            }

            fireAction = playerInput.actions["Fire"];
            if (fireAction == null) {
                Debug.LogWarning("[Grenade] Fire action not found in InputActions!");
                return;
            }

            fireAction.started += OnFireStarted;
            fireAction.canceled += OnFireCanceled;
        }

        /// <summary>
        /// Unsubscribes from Fire input to prevent callbacks after deselection.
        /// </summary>
        private void UnsubscribeFromFireInput() {
            if (fireAction == null) return;

            fireAction.started -= OnFireStarted;
            fireAction.canceled -= OnFireCanceled;
            fireAction = null;
        }

        /// <summary>
        /// Called when fire button is pressed (InputActionPhase.Started).
        /// Pulls pin and enters Pinned state.
        /// </summary>
        private void OnFireStarted(InputAction.CallbackContext context) {
            if (this == null) return;

            if (currentState != GrenadeState.Idle) {
                return;
            }

            Character character = GetComponentInParent<Character>();
            if (character != null && character.IsInterfaceMode())
                return;

            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;

            EnsureAudioService();

            currentState = GrenadeState.Pinned;
            PlayPinPullSound();
        }

        /// <summary>
        /// Called when fire button is released (InputActionPhase.Canceled).
        /// Throws grenade and starts detonation countdown.
        /// </summary>
        private void OnFireCanceled(InputAction.CallbackContext context) {
            if (this == null) return;

            if (currentState != GrenadeState.Pinned) {
                return;
            }

            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) {
                currentState = GrenadeState.Idle;
                return;
            }

            if (!CanBeUsed()) {
                currentState = GrenadeState.Idle;
                return;
            }

            if (PlayerProgress.Instance != null && PlayerProgress.Instance.GetItemTotal(GetItemID()) <= 0) {
                currentState = GrenadeState.Idle;
                return;
            }

            currentState = GrenadeState.Thrown;

            ThrowGrenade();
            PlayThrowSound();

            if (PlayerProgress.Instance != null &&
                PlayerProgress.Instance.GetItemTotal(GetItemID()) > 0) {
                currentState = GrenadeState.Idle;
                SubscribeToFireInput();
            }
        }

        #endregion

        #region THROW LOGIC

        /// <summary>
        /// Instantiates grenade prefab and applies initial velocity.
        /// </summary>
        private void ThrowGrenade() {
            if (grenadePrefab == null) {
                Debug.LogWarning("[Grenade] grenadePrefab is null!", gameObject);
                currentState = GrenadeState.Idle;
                return;
            }

            Character character = GetComponentInParent<Character>();
            if (character == null) {
                Debug.LogWarning("[Grenade] Character not found!");
                currentState = GrenadeState.Idle;
                return;
            }

            Transform cameraTransform = character.GetCameraWorld().transform;
            if (cameraTransform == null) {
                Debug.LogWarning("[Grenade] Camera not found!");
                currentState = GrenadeState.Idle;
                return;
            }

            thrownGrenadeInstance = Instantiate(grenadePrefab, cameraTransform.position, Quaternion.identity);

            Rigidbody rb = thrownGrenadeInstance.GetComponent<Rigidbody>();
            if (rb != null) {
                rb.linearVelocity = cameraTransform.forward * throwForce;
            }

            if (PlayerProgress.Instance != null) {
                PlayerProgress.Instance.UseItem(GetItemID(), 1);
                int remaining = PlayerProgress.Instance.GetItemTotal(GetItemID());

                if (remaining > 0) {
                    PlayerProgress.Instance.SetItemCurrent(GetItemID(), 1);
                } else {
                    PlayerProgress.Instance.SetItemCurrent(GetItemID(), 0);
                    EquipWeaponAutomatically();
                }
            }
        }

        #endregion

        #region AUDIO

        /// <summary>
        /// Ensures audioService is cached. If null, re-caches from ServiceLocator.
        /// Handles cases where AudioManagerService may be destroyed and recreated.
        /// </summary>
        private void EnsureAudioService()
        {
            if (audioService == null)
            {
                audioService = ServiceLocator.Current.Get<IAudioManagerService>();
            }
        }

        private void PlayEquipSound() {
            EnsureAudioService();

            if (equipClip != null && audioService != null) {
                audioService.PlaySFX2D(equipClip, equipVolume);
            }
        }

        private void PlayPinPullSound() {
            EnsureAudioService();

            if (pinPullClip != null && audioService != null) {
                audioService.PlaySFX2D(pinPullClip, pinPullVolume);
            }
        }

        private void PlayThrowSound() {
            EnsureAudioService();

            if (throwClip != null && audioService != null) {
                audioService.PlaySFX2D(throwClip, throwVolume);
            }
        }

        #endregion

        #region HELPER METHODS

        /// <summary>
        /// Automatically equips the default weapon (pistol) when grenade quantity reaches zero.
        /// Uses smooth animation transition via Character.TryRestoreWeaponSmoothly().
        /// </summary>
        private void EquipWeaponAutomatically() {
            Character character = GetComponentInParent<Character>();
            if (character == null) return;

            character.TryRestoreWeaponSmoothly();
        }

        #endregion

        #region DEBUG

        #endregion

        #endregion
    }
}
