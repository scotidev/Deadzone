using TMPro;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Interface {
    /// <summary>
    /// Interface component that hides or shows the tutorial text based on input.
    /// </summary>
    public class TextTutorial : ElementText {

        #region SERIALIZED FIELDS

        [Header("References")]

        [Tooltip("Tutorial prompt text.")]
        [SerializeField]
        private TextMeshProUGUI prompt;

        [Tooltip("Tutorial text.")]
        [SerializeField]
        private TextMeshProUGUI tutorial;

        #endregion

        #region UNITY

        protected override void Awake() {
            base.Awake();

            prompt.enabled = true;
            tutorial.enabled = false;
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Updates the visibility of prompt and tutorial text based on character state.
        /// </summary>
        protected override void Tick() {
            bool isVisible = playerCharacter.IsTutorialTextVisible();
            prompt.enabled = !isVisible;
            tutorial.enabled = isVisible;
        }

        #endregion
    }
}
