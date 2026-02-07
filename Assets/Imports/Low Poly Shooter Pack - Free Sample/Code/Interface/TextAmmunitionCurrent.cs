// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;
using System.Globalization;

namespace InfimaGames.LowPolyShooterPack.Interface
{
    /// <summary>
    /// Texto da Interface (HUD) que mostra a munição atual no pente.
    /// </summary>
    public class TextAmmunitionCurrent : ElementText
    {
        #region FIELDS SERIALIZED

        [Header("Cores")]

        [Tooltip("Se marcado, a cor do texto mudará conforme a munição acaba.")]
        [SerializeField]
        private bool updateColor = true;

        [Tooltip("Velocidade da mudança de cor.")]
        [SerializeField]
        private float emptySpeed = 1.5f;

        [Tooltip("Cor usada quando a munição está acabando.")]
        [SerializeField]
        private Color emptyColor = Color.red;

        #endregion

        #region METHODS

        /// <summary>
        /// Tick é chamado constantemente para atualizar o texto.
        /// </summary>
        protected override void Tick()
        {
            // Pega a munição atual da arma equipada.
            float current = equippedWeapon.GetAmmunitionCurrent();
            // Pega a capacidade total do pente.
            float total = equippedWeapon.GetAmmunitionTotal();

            // Atualiza o texto na tela.
            textMesh.text = current.ToString(CultureInfo.InvariantCulture);

            // Lógica para mudar a cor para vermelho quando houver poucas balas.
            if (updateColor)
            {
                // Calcula a "transparência" da cor baseada na porcentagem de balas restantes.
                float colorAlpha = (current / total) * emptySpeed;
                // Faz a transição suave entre branco (cheio) e vermelho (vazio).
                textMesh.color = Color.Lerp(emptyColor, Color.white, colorAlpha);
            }
        }

        #endregion
    }
}