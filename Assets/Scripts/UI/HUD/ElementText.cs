using TMPro;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Interface {
    /// <summary>
    /// Text Interface Element. Inherits from Element and adds a TextMeshProUGUI component for displaying text in the HUD.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public abstract class ElementText : Element {

        #region FIELDS

        protected TextMeshProUGUI textMesh;

        #endregion

        #region UNITY

        protected override void Awake() {
            base.Awake();

            textMesh = GetComponent<TextMeshProUGUI>();
        }

        #endregion
    }
}
