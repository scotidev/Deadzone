using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Deadzone.UI {
    /// <summary>
    /// Visual feedback effect displayed on screen edges when player heals.
    /// Shows a green vignette that pulses on screen edges.
    /// </summary>
    public class HealFeedbackUI : MonoBehaviour {

        #region SERIALIZED FIELDS

        [Header("Vignette Image")]
        [Tooltip("Arraste aqui o objeto Image que contém a vignette")]
        [SerializeField] private Image vignetteImage;

        [Header("Settings")]
        [SerializeField] private float pulseSpeed = 8f;
        [SerializeField] private float maxAlpha = 0.8f;

        #endregion

        #region FIELDS

        private CanvasGroup canvasGroup;
        private Coroutine currentCoroutine;

        #endregion

        #region UNITY

        private void Awake() {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0f;

            if (vignetteImage != null) {
                vignetteImage.gameObject.SetActive(false);
            }
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Shows the heal feedback effect for the specified duration.
        /// </summary>
        /// <param name="duration">How long the effect lasts in seconds.</param>
        public void Show(float duration) {
            if (vignetteImage == null) {
                Debug.LogWarning("[HealFeedbackUI] vignetteImage não atribuída no Inspector!");
                return;
            }

            if (currentCoroutine != null) {
                StopCoroutine(currentCoroutine);
            }
            currentCoroutine = StartCoroutine(ShowEffect(duration));
        }

        private IEnumerator ShowEffect(float duration) {
            vignetteImage.gameObject.SetActive(true);

            float elapsed = 0f;

            while (elapsed < duration) {
                elapsed += Time.deltaTime;

                float pulse = Mathf.Sin(elapsed * pulseSpeed) * 0.5f + 0.5f;
                float alpha = pulse * maxAlpha;

                canvasGroup.alpha = alpha;

                yield return null;
            }

            canvasGroup.alpha = 0f;
            vignetteImage.gameObject.SetActive(false);
        }

        #endregion
    }
}