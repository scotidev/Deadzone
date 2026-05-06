// Copyright 2021, Infima Games. All Rights Reserved.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// funcionam com o mesmo padrão: GameObjects filhos de Inventory, cada um com seu ItemBehaviour.
// Seleção funciona para TODOS os tipos de itens (armas, consumíveis, buildables) apertando 1-8.
// Sistema segue padrão Infima Games mantendo GameObjects filhos + components reutilizáveis.

// LEMBRANDO: SÃO 8 ITEMS! o item 9 na verdade é o colete (Vest), que nao pode ser segurando na mao, 
// é só uma vestimenta. Vest é equipado automaticamente e não está nesta seleção.

namespace InfimaGames.LowPolyShooterPack {
    public class Inventory : InventoryBehaviour {

        #region SERIALIZED FIELDS

        [Tooltip("Reference to the BuildingController for placement logic.")]
        [SerializeField] private BuildingController buildingController;

        #endregion

        #region FIELDS

        // REFATORAÇÃO: ItemBehaviour[] unifica todos os tipos de items (weapons, consumables, buildables)
        private ItemBehaviour[] selectableItems;
        private ItemBehaviour currentlySelected;
        private int currentSelectionIndex = -1;

        // Para compatibilidade com Character.TryEquipWeapon() - mantém referência às armas
        private WeaponBehaviour[] weapons;
        private WeaponBehaviour equipped;
        private int equippedIndex = -1;

        private Character character;

        // Mapeia teclas 1-8 aos índices no array selectableItems
        private readonly Dictionary<int, int> keyToIndex = new() {
            { 1, 0 }, { 2, 1 }, { 3, 2 }, { 4, 3 },
            { 5, 4 }, { 6, 5 }, { 7, 6 }, { 8, 7 }
        };

        #endregion

        #region UNITY

        private void Awake() {
            ResolvePlayerCharacter();
        }

        #endregion

        #region METHODS

        public override void Init(int equippedAtStart = 0) {
            // CONCEITO: GetComponentsInChildren<ItemBehaviour>(true) busca TODOS os components ItemBehaviour
            // nos GameObjects filhos, independente de qual tipo específico (WeaponBehaviour, ConsumableBehaviour, BuildableBehaviour).
            // Isso unifica a busca de todos os 8 items em uma única operação.
            ItemBehaviour[] allItems = GetComponentsInChildren<ItemBehaviour>(true);
            
            // CONCEITO: Filtrar para incluir APENAS itens que não são Vest.
            // Vest é um item passivo que fica sempre equipado e não participa da seleção 1-8.
            var filteredItems = new List<ItemBehaviour>();
            for (int i = 0; i < allItems.Length; i++) {
                if (allItems[i] is Vest) {
                    Debug.Log($"  [Skipped] {allItems[i].GetDisplayName()} (Vest - passive, not selectable)");
                    continue;
                }
                filteredItems.Add(allItems[i]);
            }
            selectableItems = filteredItems.ToArray();

            for (int i = 0; i < selectableItems.Length; i++) {
                Debug.Log($"  [{i}] {selectableItems[i].GetDisplayName()} (ID: {selectableItems[i].GetItemID()})");
            }

            // COMPATIBILIDADE: Também buscamos WeaponBehaviour para manter compatibilidade com Character
            // Mas aqui também precisamos ignorar a Vest (já que WeaponBehaviour ≠ Vest)
            WeaponBehaviour[] allWeapons = GetComponentsInChildren<WeaponBehaviour>(true);
            var filteredWeapons = new List<WeaponBehaviour>();
            for (int i = 0; i < allWeapons.Length; i++) {
                filteredWeapons.Add(allWeapons[i]);
            }
            weapons = filteredWeapons.ToArray();

            // Desativa todos os items no início
            foreach (ItemBehaviour item in selectableItems)
                item.gameObject.SetActive(false);

            // Seleciona o primeiro item (Pistola por padrão)
            SelectItem(equippedAtStart);
        }

        /// <summary>
        /// Called by Input System when any numeric key (1-8) is pressed.
        /// Unified handling for all item types (weapons, consumables, buildables).
        /// </summary>
        public void OnSelectItem(InputAction.CallbackContext context) {
            if (context.phase != InputActionPhase.Performed)
                return;

            if (character != null && character.IsInterfaceMode())
                return;

            // CONCEITO: Extrai o número da tecla do caminho do Input System.
            // Exemplo: "<Keyboard>/1" → pega último char '1' → converte para int 1
            string path = context.control.path;
            char digitChar = path[path.Length - 1];
            int keyNumber = digitChar - '0';

            SelectByKeyNumber(keyNumber);
        }

        /// <summary>
        /// Select item by key number (1-8). Unified logic for all item types.
        /// </summary>
        private void SelectByKeyNumber(int keyNumber) {
            // Valida se a tecla está no range 1-8
            if (!keyToIndex.TryGetValue(keyNumber, out int itemIndex)) {
                return;
            }

            if (itemIndex < 0 || itemIndex >= selectableItems.Length) {
                return;
            }

            SelectItem(itemIndex);
        }

        /// <summary>
        /// Select item by index. Deselects previous item and selects new one.
        /// REFATORAÇÃO: Método unificado que funciona para armas, consumíveis e buildables.
        /// </summary>
        private void SelectItem(int index) {
            if (selectableItems == null || index < 0 || index >= selectableItems.Length) {
                return;
            }

            ItemBehaviour newItem = selectableItems[index];

            if (newItem == null) {
                return;
            }

            // SAFETY: Always allow first item (Pistol/index 0) to be selected, even during Init()
            // This ensures player always has a weapon equipped at game start.
            // For other items, validate via CanBeUsed() check.
            if (index != 0 && !newItem.CanBeUsed()) {
                return;
            }


            // Deseleciona item atual (se houver)
            if (currentlySelected != null) {
                currentlySelected.OnDeselected();
                currentlySelected.gameObject.SetActive(false);
            }

            // Seleciona novo item
            currentlySelected = newItem;
            currentSelectionIndex = index;
            currentlySelected.gameObject.SetActive(true);
            currentlySelected.OnSelected();

            // Se for arma, também atualiza compatibilidade com Character
            if (newItem is WeaponBehaviour weapon) {
                UpdateEquippedWeapon(weapon);
            }
        }

        /// <summary>
        /// Updates equipped weapon reference for Character compatibility.
        /// Mantém compatibilidade com Character.TryEquipWeapon() e sistema de armas existente.
        /// </summary>
        /// <summary>
        /// Updates equipped weapon reference for Character compatibility.
        /// Mantém compatibilidade com Character.TryEquipWeapon() e sistema de armas existente.
        /// </summary>
        private void UpdateEquippedWeapon(WeaponBehaviour weapon) {
            if (weapons == null) {
                return;
            }


            // Encontra o índice da arma no array weapons
            for (int i = 0; i < weapons.Length; i++) {
                if (weapons[i] == weapon) {
                    equippedIndex = i;
                    equipped = weapon;

                    // SINCRONIZAÇÃO: Força o Character a atualizar suas referências imediatamente
                    if (character != null) {
                        character.RefreshWeaponSetup();
                    } else {
                    }
                    return;
                }
            }

        }

        /// <summary>
        /// Restores the last equipped weapon after buildable placement is canceled.
        /// Called by BuildingController when player finishes placing a buildable.
        /// </summary>
        public void RestoreLastWeapon() {
            // Find the first weapon that is unlocked and select it
            // Priority: Try Pistol (index 0) first, then other weapons
            for (int i = 0; i < selectableItems.Length; i++) {
                ItemBehaviour item = selectableItems[i];
                if (item is WeaponBehaviour weapon && (i == 0 || weapon.CanBeUsed())) {
                    SelectItem(i);
                    return;
                }
            }

        }

        /// <summary>
        /// Resolves the Character reference needed for weapon selection.
        /// Uses GetComponentInParent to find Character in the parent hierarchy.
        /// </summary>
        private void ResolvePlayerCharacter() {
            if (character == null) {
                character = GetComponentInParent<Character>();
            }
        }

        // OBSOLETO: SelectBuildable(), SelectBuildableItem(), SelectWeapon() - PODEM SER DELETADOS
        // Estes métodos foram substituídos pelo sistema unificado SelectItem().
        // A lógica de buildables agora é gerenciada por BuildableBehaviour.OnSelected().
        // A lógica de armas agora é gerenciada por WeaponBehaviour.OnSelected().

        public override WeaponBehaviour Equip(int index) {
            if (weapons == null || index > weapons.Length - 1 || equippedIndex == index)
                return equipped;

            if (index != 0 && PlayerProgress.Instance != null) {
                string weaponID = GetWeaponIDForIndex(index);
                if (!string.IsNullOrEmpty(weaponID) && !PlayerProgress.Instance.IsWeaponUnlocked(weaponID))
                    return equipped;
            }

            if (equipped != null)
                equipped.gameObject.SetActive(false);

            equippedIndex = index;
            equipped = weapons[equippedIndex];
            equipped.gameObject.SetActive(true);

            return equipped;
        }

        #endregion

        #region GETTERS

        public override int GetLastIndex() {
            int newIndex = equippedIndex - 1;
            if (newIndex < 0)
                newIndex = weapons.Length - 1;
            return newIndex;
        }

        public override int GetNextIndex() {
            int newIndex = equippedIndex + 1;
            if (newIndex > weapons.Length - 1)
                newIndex = 0;
            return newIndex;
        }

        public override WeaponBehaviour GetEquipped() => equipped;
        public override int GetEquippedIndex() => equippedIndex;

        /// <summary>
        /// Maps weapon array index to weapon ID for unlock checking.
        /// This mapping should match the shop item order.
        /// </summary>
        public string GetWeaponIDForIndex(int index) {
            switch (index) {
                case 0: return "1"; // Pistol
                case 1: return "2"; // AK47
                case 2: return "3"; // Shotgun
                case 3: return "4"; // Medkit
                case 4: return "5"; // Grenades
                default: return null;
            }
        }

        /// <summary>
        /// Selects the weapon at the currently equipped index.
        /// </summary>
        public void ReEquipCurrentItem() {
            Equip(equippedIndex);
        }

        #endregion
    }
}