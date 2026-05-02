// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;
using UnityEngine.InputSystem;

// RESOLVER BUG: seleção de items nao esta funcionando corretamente. Temos que garantir que funcione tanto para armas quanto para itens de consturção, medkits, granadas, ... TUDO. e deve funcionar asssim: temos um gameobject prefab desativado aninhado ao game object Inventory que está aninhado em Player. Ai quando selecionarmos o item, seja ele independente d equal for, esse gameobject é ativado, e o item aparece na mão do player etc. Como nao temos modelo para todos os items 3d ainda, eu coloco placeholders como cubos e cilindron. O importante é garantir que a lógica de seleção funcione para todos os tipos de itens, e que o sistema seja flexível para acomodar diferentes categorias de itens (armas, consumíveis, buildables) sem bugs.
// LEMBRANDO: SÃO 8 ITEMS! o item 9 na verdade é o colete, que nao pode ser segurando na mao, é so uma vestimenta.

namespace InfimaGames.LowPolyShooterPack {
    public class Inventory : InventoryBehaviour {

        #region SERIALIZED FIELDS

        [Tooltip("Reference to the BuildingController for placement logic.")]
        [SerializeField] private BuildingController buildingController;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        #endregion

        #region FIELDS

        private WeaponBehaviour[] weapons;
        private WeaponBehaviour equipped;
        private int equippedIndex = -1;
        private Character character;
        private readonly string[] itemNames = {
            "Pistol",         // Key 1 (Index 0)
            "AK47",           // Key 2 (Index 1)
            "Shotgun",        // Key 3 (Index 2)
            "Med Kit",        // Key 4 (Index 3)
            "Grenade",       // Key 5 (Index 4)
            "Barricade",     // Key 6 (Buildable 1)
            "Explosive Barrel", // Key 7 (Buildable 2)
            "Bear Trap",     // Key 8 (Buildable 3)
        };

        #endregion

        #region UNITY

        private void Awake() {
            ResolvePlayerCharacter();
        }

        #endregion

        #region METHODS

        public override void Init(int equippedAtStart = 0) {

            weapons = GetComponentsInChildren<WeaponBehaviour>(true);

            if (enableDebugLogs) {
                Debug.Log($"[Inventory.Init] Called with equippedAtStart = {equippedAtStart}");
                Debug.Log($"[Inventory.Init] Found {weapons.Length} weapons:");
                for (int i = 0; i < weapons.Length; i++) {
                    Debug.Log($"  [{i}] {weapons[i].name}");
                }
            }

            foreach (WeaponBehaviour weapon in weapons)
                weapon.gameObject.SetActive(false);

            if (enableDebugLogs) {
                Debug.Log($"[Inventory.Init] Now calling Equip({equippedAtStart})");
            }
            Equip(equippedAtStart);
        }

        /// <summary>
        /// Called by Input System when any numeric key (1-8) is pressed.
        /// Handles both weapons and buildables in a unified way.
        /// </summary>
        public void OnSelectItem(InputAction.CallbackContext context) {
            if (context.phase != InputActionPhase.Performed)
                return;

            if (character != null && character.IsInterfaceMode())
                return;

            string path = context.control.path;
            char digitChar = path[path.Length - 1];
            int keyNumber = digitChar - '0';

            SelectByKeyNumber(keyNumber);
        }

        /// <summary>
        /// Unified item selection by key number (1-8).
        /// Keys 1-5 select weapons, NOTA: SO TEM 3 ARMAS, 1. PISTOL 2. AK47 3.SHOTGUN, O 4. É MEDKIT E O 5. É GRANADA. Keys 6-8 select buildables.
        /// </summary>
        private void SelectByKeyNumber(int keyNumber) {
            if (keyNumber < 1 || keyNumber > 8) {
                Debug.LogWarning($"[Inventory.SelectByKeyNumber] Invalid key number: {keyNumber}. Expected 1-8.");
                return;
            }

            if (keyNumber >= 6 && keyNumber <= 8) {
                SelectBuildable(keyNumber);
            } else {
                SelectWeapon(keyNumber);
            }
        }

        /// <summary>
        /// Handles buildable item selection.
        /// </summary>
        private void SelectBuildable(int keyNumber) {
            if (buildingController == null) {
                buildingController = FindFirstObjectByType<BuildingController>();
                if (buildingController == null) {
                    Debug.LogError("[Inventory.SelectBuildable] Cannot find BuildingController!");
                    return;
                }
            }

            int buildableSlot = keyNumber - 5;
            BuildableDataSO selectedBuildable = buildableSlot switch {
                1 => buildingController.Barricade,
                2 => buildingController.ExplosiveBarrel,
                3 => buildingController.BearTrap,
                _ => null
            };

            if (selectedBuildable == null) return;

            SelectBuildableItem(selectedBuildable);
        }

        private void SelectBuildableItem(BuildableDataSO buildable) {
            ResolvePlayerCharacter();

            if (buildable == null) return;

            if (PlayerProgress.Instance != null) {
                string buildableID = buildingController.GetBuildableID(buildable);
                if (!string.IsNullOrEmpty(buildableID)) {
                    int quantity = PlayerProgress.Instance.GetBuildableQuantity(buildableID);
                    if (quantity <= 0) {
                        Debug.LogWarning($"[Inventory] No {buildableID} in inventory! Purchase from shop first.");
                        return;
                    }
                }
            }

            if (buildingController.IsPlacing && buildingController.CurrentSelectedItem == buildable) {
                buildingController.CancelCurrentPlacement();
                return;
            }

            buildingController.StartPlacement(buildable);
            character?.SetHolstered(true);
        }

        private void ResolvePlayerCharacter() {
            if (character == null) {
                character = GetComponent<Character>();
            }
            if (character == null) {
                character = FindFirstObjectByType<Character>();
            }
            if (character == null) {
                Debug.LogWarning("[Inventory.ResolvePlayerCharacter] Could not find Character!");
            }
        }

        /// <summary>
        /// Handles weapon/item selection.
        /// </summary>
        private void SelectWeapon(int keyNumber) {
            if (BuildingController.Instance != null && BuildingController.Instance.IsPlacing) {
                BuildingController.Instance.CancelPlacement();
            }

            int weaponIndex;
            if (keyNumber <= 5) {
                weaponIndex = keyNumber - 1;
            } else {
                weaponIndex = 8;
            }

            if (equippedIndex == weaponIndex) {
                return;
            }

            if (weaponIndex != 0 && PlayerProgress.Instance != null) {
                string weaponID = GetWeaponIDForIndex(weaponIndex);
                if (!string.IsNullOrEmpty(weaponID) && !PlayerProgress.Instance.IsWeaponUnlocked(weaponID)) {
                    return;
                }
            }

            if (character != null) {
                character.TryEquipWeapon(weaponIndex);
            } else {
                Equip(weaponIndex);
            }
        }

        public override WeaponBehaviour Equip(int index) {
            if (weapons == null) {
                Debug.LogWarning("[Inventory.Equip] weapons array is null!");
                return equipped;
            }

            if (index > weapons.Length - 1) {
                Debug.LogWarning($"[Inventory.Equip] Index {index} is out of bounds (max = {weapons.Length - 1})");
                return equipped;
            }

            if (equippedIndex == index) {
                return equipped;
            }

            if (index != 0 && PlayerProgress.Instance != null) {
                string weaponID = GetWeaponIDForIndex(index);
                if (!string.IsNullOrEmpty(weaponID) && !PlayerProgress.Instance.IsWeaponUnlocked(weaponID)) {
                    return equipped;
                }
            }

            if (equipped != null) {
                equipped.gameObject.SetActive(false);
            }

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
                // Buildables (indices 5-7) are handled by BuildingController
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