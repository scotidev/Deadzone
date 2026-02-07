// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Gerencia as armas que o jogador possui e qual está ativa.
    /// </summary>
    public class Inventory : InventoryBehaviour
    {
        #region FIELDS

        /// <summary>
        /// Lista de todas as armas disponíveis no inventário. 
        /// Elas são encontradas automaticamente como objetos filhos deste script na Unity.
        /// </summary>
        private WeaponBehaviour[] weapons;

        /// <summary>
        /// A arma que está atualmente nas mãos do jogador.
        /// </summary>
        private WeaponBehaviour equipped;
        /// <summary>
        /// O índice (número) da arma equipada na lista.
        /// </summary>
        private int equippedIndex = -1;

        #endregion

        #region METHODS

        /// <summary>
        /// Inicializa o inventário, encontrando as armas e equipando a primeira.
        /// </summary>
        public override void Init(int equippedAtStart = 0)
        {
            // Busca todas as armas que são filhas deste objeto.
            weapons = GetComponentsInChildren<WeaponBehaviour>(true);

            // Desativa todas as armas visualmente no início.
            foreach (WeaponBehaviour weapon in weapons)
                weapon.gameObject.SetActive(false);

            // Equipa a arma inicial (geralmente a de índice 0).
            Equip(equippedAtStart);
        }

        /// <summary>
        /// Troca a arma atual por uma nova baseada no índice.
        /// </summary>
        public override WeaponBehaviour Equip(int index)
        {
            // Se não houver armas, não faz nada.
            if (weapons == null)
                return equipped;

            // Verifica se o índice pedido existe na lista.
            if (index > weapons.Length - 1)
                return equipped;

            // Se já estiver com essa arma, não faz nada.
            if (equippedIndex == index)
                return equipped;

            // Desativa a arma que estava na mão antes.
            if (equipped != null)
                equipped.gameObject.SetActive(false);

            // Atualiza o índice e a referência da arma atual.
            equippedIndex = index;
            equipped = weapons[equippedIndex];

            // Ativa visualmente a nova arma.
            equipped.gameObject.SetActive(true);

            return equipped;
        }

        #endregion

        #region Getters

        public override int GetLastIndex()
        {
            //Get last index with wrap around.
            int newIndex = equippedIndex - 1;
            if (newIndex < 0)
                newIndex = weapons.Length - 1;

            //Return.
            return newIndex;
        }

        public override int GetNextIndex()
        {
            //Get next index with wrap around.
            int newIndex = equippedIndex + 1;
            if (newIndex > weapons.Length - 1)
                newIndex = 0;

            //Return.
            return newIndex;
        }

        public override WeaponBehaviour GetEquipped() => equipped;
        public override int GetEquippedIndex() => equippedIndex;

        #endregion
    }
}