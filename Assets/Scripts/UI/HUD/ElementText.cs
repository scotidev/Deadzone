// Copyright 2021, Infima Games. All Rights Reserved.

using TMPro;
using UnityEngine;

// REFATORAÇÃO: precisamos arrumar os scripts de HUD de texto para herdarem desse script, como CurrencyUI por exemplo ou ShopUI, ou qualquer outro que esteja usando TextMeshProUGUI. Assim, evitamos ter que ficar pegando o componente toda hora e deixamos o código mais limpo.

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