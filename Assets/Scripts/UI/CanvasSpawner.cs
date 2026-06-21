using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Interface {
    /// <summary>
    /// Player Interface.
    /// </summary>
    public class CanvasSpawner : MonoBehaviour {
        #region SERIALIZED FIELDS

        [Header("Settings")]

        [Tooltip("Canvas prefab spawned at start. Displays the player's user interface.")]
        [SerializeField]
        private GameObject canvasPrefab;

        #endregion

        #region UNITY

        private void Awake() {
            Instantiate(canvasPrefab);
        }

        #endregion
    }
}
