// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

// Refatoração: Usar um prefab ou manter o canvas na scene? Como isos interfere nos Panels que herdam de BaseUI além do HUD do player? talvez usar o HUD de player aqui

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