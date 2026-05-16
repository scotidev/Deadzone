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
        private float[] initialAlphas;
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

            // Salva os alphas configurados no Inspector antes de esconder
            initialAlphas = new float[graphics.Length];
            for (int i = 0; i < graphics.Length; i++)
                initialAlphas[i] = graphics[i].color.a;

            foreach (Graphic g in graphics)
                g.color = new Color(g.color.r, g.color.g, g.color.b, 0f);
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Shows the tutorial panel with fade in effect and plays the universal sound.
        /// Fadeia cada graphic de 0 até seu alpha definido no Inspector.
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

            // Fade de 0 até o alpha de cada graphic definido no Inspector
            float[] from = new float[graphics.Length];
            fadeRoutine = StartCoroutine(FadeRoutine(from, initialAlphas, fadeInDuration, null));

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
        /// Fadeia cada graphic do alpha atual até 0.
        /// </summary>
        public void StartFadeOut(Action onComplete) {
            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);

            if (graphics == null)
                graphics = GetComponentsInChildren<Graphic>(true);

            float[] from = new float[graphics.Length];
            float[] to = new float[graphics.Length];
            for (int i = 0; i < graphics.Length; i++) {
                from[i] = graphics[i].color.a;
                to[i] = 0f;
            }

            fadeRoutine = StartCoroutine(FadeRoutine(from, to, fadeOutDuration, onComplete));
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

        private IEnumerator FadeRoutine(float[] from, float[] to, float duration, Action onComplete) {
            float elapsed = 0f;

            while (elapsed < duration) {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                for (int i = 0; i < graphics.Length; i++)
                    graphics[i].color = new Color(graphics[i].color.r, graphics[i].color.g, graphics[i].color.b, Mathf.Lerp(from[i], to[i], t));

                yield return null;
            }

            for (int i = 0; i < graphics.Length; i++)
                graphics[i].color = new Color(graphics[i].color.r, graphics[i].color.g, graphics[i].color.b, to[i]);

            onComplete?.Invoke();
        }

        #endregion

    }

}
