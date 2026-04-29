// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

// PQ NO JOGO TEMOS MUNIÇÃO INFINITA? TEMOS QUE ATUALIZAR PARA QUE FUNCIONE COM O SISTEMA DE LOJA, COMPRA  DE MUNIÇÃO, LIMITES  DE MUNIÇÃO DO PROJETO... DEVEMOS RESPEITAR O LIMITE IMPOSTO PELOS SCRIPTABLE OBJECTS DE  WEAPON?

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Magazine.
    /// </summary>
    public class Magazine : MagazineBehaviour {

        #region SERIALIZED FIELDS

        [Header("Settings")]

        [SerializeField] private int ammunitionTotal = 10;
        [SerializeField] private Sprite sprite;

        #endregion

        #region GETTERS

        public override int GetAmmunitionTotal() => ammunitionTotal;
        public override Sprite GetSprite() => sprite;

        #endregion
    }
}