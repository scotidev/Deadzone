using System;
using System.Collections;
using InfimaGames.LowPolyShooterPack;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IAudio = InfimaGames.LowPolyShooterPack.IAudioManagerService;

namespace Deadzone.UI {

    /// <summary>
    /// Controls the visual display of tutorials on the HUD.
    /// Attach this to the TutorialBackground Image (child of Canvas > HUD).
    /// Manages showing/hiding with fade in/out on all child Graphics and audio.
    /// </summary>
    public class TutorialUI : MonoBehaviour {

        #region SERIALIZED FIELDS

        [Header("References")]
        [SerializeField] private TextMeshProUGUI tutorialText;
        [SerializeField] private Image tutorialImage;

        [Header("Fade Settings")]
        [Tooltip("Duration of the fade in effect when a tutorial appears.")]
        [SerializeField] private float fadeInDuration = 0.3f;

        [Tooltip("Duration of the fade out effect when a tutorial ends.")]
        [SerializeField] private float fadeOutDuration = 3f;

        [Header("Timeout")]
        [Tooltip("Default time in seconds before a tutorial step auto-advances.")]
        [SerializeField] private float defaultStepTimeout = 8f;

        [Header("Audio")]
        [Tooltip("Universal sound played every time a tutorial appears.")]
        [SerializeField] private AudioClip showSound;
        [SerializeField] private float soundVolume = 1f;

        #endregion

        #region FIELDS

        private Graphic[] graphics;
        private Coroutine fadeRoutine;
        private IAudio audioService;

        #endregion

        #region PROPERTIES

        public float DefaultStepTimeout => defaultStepTimeout;
        public float FadeOutDuration => fadeOutDuration;

        #endregion

        #region UNITY

        private void Awake() {
            graphics = GetComponentsInChildren<Graphic>(true);

            if (tutorialText == null)
                tutorialText = GetComponentInChildren<TextMeshProUGUI>();

            if (tutorialImage == null)
                tutorialImage = GetComponentInChildren<Image>();

            audioService = ServiceLocator.Current.Get<IAudio>();

            foreach (Graphic g in graphics)
                g.color = new Color(g.color.r, g.color.g, g.color.b, 0f);
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Shows the tutorial panel with fade in effect and plays the universal sound.
        /// </summary>
        public void Show(string text, Sprite image) {
            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);

            if (graphics == null)
                graphics = GetComponentsInChildren<Graphic>(true);

            if (tutorialText != null)
                tutorialText.text = text;

            if (tutorialImage != null) {
                tutorialImage.gameObject.SetActive(image != null);
                tutorialImage.sprite = image;
            }

            foreach (Graphic g in graphics)
                g.color = new Color(g.color.r, g.color.g, g.color.b, 0f);

            fadeRoutine = StartCoroutine(FadeRoutine(0f, 1f, fadeInDuration, null));

            audioService?.PlaySFX2D(showSound, soundVolume);
        }

        /// <summary>
        /// Shows the tutorial panel with text only.
        /// </summary>
        public void Show(string text) {
            Show(text, null);
        }

        /// <summary>
        /// Starts a fade out over fadeOutDuration seconds, then calls onComplete.
        /// </summary>
        public void StartFadeOut(Action onComplete) {
            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);

            if (graphics == null)
                graphics = GetComponentsInChildren<Graphic>(true);

            float currentAlpha = graphics.Length > 0 ? graphics[0].color.a : 1f;
            fadeRoutine = StartCoroutine(FadeRoutine(currentAlpha, 0f, fadeOutDuration, onComplete));
        }

        /// <summary>
        /// Immediately hides the tutorial panel without fade.
        /// </summary>
        public void Hide() {
            if (fadeRoutine != null) {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            if (graphics == null)
                graphics = GetComponentsInChildren<Graphic>(true);

            foreach (Graphic g in graphics)
                g.color = new Color(g.color.r, g.color.g, g.color.b, 0f);
        }

        private IEnumerator FadeRoutine(float from, float to, float duration, Action onComplete) {
            float elapsed = 0f;

            while (elapsed < duration) {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(from, to, elapsed / duration);

                foreach (Graphic g in graphics)
                    g.color = new Color(g.color.r, g.color.g, g.color.b, alpha);

                yield return null;
            }

            foreach (Graphic g in graphics)
                g.color = new Color(g.color.r, g.color.g, g.color.b, to);

            onComplete?.Invoke();
        }

        #endregion

    }

}
