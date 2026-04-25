// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

// REFATORAÇÃO: esse script nao é necessario, nao mostraremoso mais o timescale na tela.

namespace InfimaGames.LowPolyShooterPack.Interface {
    /// <summary>
    /// Component that changes a text to match the current time scale.
    /// </summary>
    public class TextTimescale : ElementText {
        #region METHODS

        protected override void Tick() {
            //Change text to match the time scale!
            textMesh.text = "Timescale : " + Time.timeScale;
        }

        #endregion
    }
}