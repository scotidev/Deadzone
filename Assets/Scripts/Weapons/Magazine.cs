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

        #region FIELDS

        private Weapon parentWeapon;

        #endregion

        #region UNITY

        private void Awake() {
            parentWeapon = GetComponentInParent<Weapon>();
        }

        #endregion

        #region GETTERS

        /// <summary>
        /// Returns the total ammunition capacity of this magazine.
        /// Now dynamically calculates capacity based on WeaponDataSO and upgrade level.
        /// Falls back to inspector value if weaponData is missing.
        /// </summary>
        public override int GetAmmunitionTotal() {
            if (parentWeapon != null) {
                // If the weapon has a WeaponDataSO, use the formula to get capacity for current level
                // This makes the SO the single source of truth.
                if (PlayerProgress.Instance != null) {
                    string itemID = parentWeapon.GetItemID();
                    int level = PlayerProgress.Instance.GetItemLevel(itemID);
                    
                    // We can use the formula directly from PlayerProgress which is already synced with SO
                    return PlayerProgress.Instance.GetItemMaxCurrent(itemID, level);
                }
            }
            
            return ammunitionTotal;
        }

        public override Sprite GetSprite() => sprite;

        #endregion
    }
}