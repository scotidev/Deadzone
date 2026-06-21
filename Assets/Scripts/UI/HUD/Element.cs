using UnityEngine;
using InfimaGames.LowPolyShooterPack;

namespace InfimaGames.LowPolyShooterPack.Interface {
    /// <summary>
    /// Interface Element that can be used as a base for all other elements. It also has a Tick method that is called every frame, so you can use it to update the element's state.
    /// </summary>
    public abstract class Element : MonoBehaviour {

        #region FIELDS

        protected IGameModeService gameModeService;
        protected CharacterBehaviour playerCharacter;
        protected InventoryBehaviour playerCharacterInventory;
        protected WeaponBehaviour equippedWeapon;
        protected ItemBehaviour equippedItem;

        #endregion

        #region UNITY

        protected virtual void Awake() {
            gameModeService = ServiceLocator.Current.Get<IGameModeService>();

            if (gameModeService == null) {
                Debug.LogWarning("[Element] IGameModeService not found. Canvas may have spawned before initialization.", gameObject);
                return;
            }

            playerCharacter = gameModeService.GetPlayerCharacter();

            if (playerCharacter == null) {
                Debug.LogWarning("[Element] Player character not found. Game mode may not be initialized.", gameObject);
                return;
            }

            playerCharacterInventory = playerCharacter.GetInventory();
        }

        private void Update() {
            if (Equals(playerCharacterInventory, null))
                return;

            equippedWeapon = playerCharacterInventory.GetEquipped();
            equippedItem = playerCharacterInventory.GetEquippedItem();

            Tick();
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Tick. Called every frame. Use it to update the element's state.
        /// </summary>
        protected virtual void Tick() { }

        #endregion
    }
}
