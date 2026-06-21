using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Interface {
    /// <summary>
    /// Crosshair.
    /// </summary>
    public class Crosshair : Element {

        #region SERIALIZED FIELDS

        [Header("Settings")]

        [Tooltip("Visibility changing smoothness.")]
        [SerializeField] private float smoothing = 8.0f;

        [Tooltip("Minimum scale the Crosshair needs in order to be visible. Useful to avoid weird tiny images.")]
        [SerializeField] private float minimumScale = 0.15f;

        #endregion

        #region FIELDS

        private float current = 1.0f;
        private float target = 1.0f;

        private RectTransform rectTransform;

        #endregion

        #region UNITY

        protected override void Awake() {
            base.Awake();

            rectTransform = GetComponent<RectTransform>();
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Updates the crosshair scale based on character state and visibility.
        /// </summary>
        protected override void Tick() {
            bool visible = playerCharacter.IsCrosshairVisible();
            target = visible ? 1.0f : 0.0f;

            current = Mathf.Lerp(current, target, Time.deltaTime * smoothing);
            rectTransform.localScale = Vector3.one * current;

            for (var i = 0; i < transform.childCount; i++)
                transform.GetChild(i).gameObject.SetActive(current > minimumScale);
        }

        #endregion
    }
}
