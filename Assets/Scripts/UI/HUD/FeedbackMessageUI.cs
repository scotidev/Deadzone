using UnityEngine;
using TMPro;
using InfimaGames.LowPolyShooterPack;
using IAudio = InfimaGames.LowPolyShooterPack.IAudioManagerService;

namespace Deadzone.UI {

    /// <summary>
    /// Displays a temporary feedback message (e.g. "Out of items!") with auto-hide and optional audio feedback.
    /// Attach to a parent Image in Canvas > HUD > Feedback. Child TextMeshPro shows the message.
    /// </summary>
    public class FeedbackMessageUI : MonoBehaviour {

        #region SINGLETON

        public static FeedbackMessageUI Instance { get; private set; }

        #endregion

        #region SERIALIZED FIELDS

        [Header("References")]
        [SerializeField] private TMP_Text messageText;

        [Header("Settings")]
        [SerializeField] private float displayDuration = 1.5f;

        [Header("Audio")]
        [SerializeField] private AudioClip outOfAmmoClip;
        [SerializeField] private float audioVolume = 1f;

        #endregion

        #region PRIVATE FIELDS

        private IAudio audioService;
        private Coroutine autoHideRoutine;

        #endregion

        #region UNITY LIFECYCLE

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            audioService = ServiceLocator.Current.Get<IAudio>();

            ForceHide();
        }

        private void OnDestroy() {
            if (Instance == this)
                Instance = null;
        }

        #endregion

        #region PUBLIC METHODS

        /// <summary>
        /// Shows the feedback message, plays the audio clip, and auto-hides after displayDuration.
        /// Safe to call multiple times — restarts the timer if already visible.
        /// </summary>
        public void Show() {
            if (autoHideRoutine != null)
                StopCoroutine(autoHideRoutine);

            gameObject.SetActive(true);

            audioService?.PlaySFX2D(outOfAmmoClip, audioVolume);

            autoHideRoutine = StartCoroutine(HideAfterDelay());
        }

        /// <summary>
        /// Immediately hides the feedback message without playing audio.
        /// </summary>
        public void ForceHide() {
            if (autoHideRoutine != null)
                StopCoroutine(autoHideRoutine);

            gameObject.SetActive(false);
        }

        #endregion

        #region PRIVATE METHODS

        private System.Collections.IEnumerator HideAfterDelay() {
            yield return new WaitForSeconds(displayDuration);
            ForceHide();
        }

        #endregion

    }
}
