// Copyright 2021, Infima Games. All Rights Reserved.

// DÚVIDA: esse script de fato bloqueia o mouse ou não? o que ele faz? Se ele for importante pra gameplay de fato, precisamos manter ele, mas se seu unico objetivo for mostrar na tela se o mouse esta bloqueado ou não, é desnecessario.

namespace InfimaGames.LowPolyShooterPack.Interface {
    /// <summary>
    /// This component handles warning developers whether their mouse is locked or not by
    /// updating a text in the interface.
    /// </summary>
    public class TextMouseLock : ElementText {
        #region METHODS

        protected override void Tick() {
            textMesh.text = "Cursor " + (playerCharacter.IsCursorLocked() ? "Locked" : "Unlocked");
        }

        #endregion
    }
}