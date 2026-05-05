// Copyright 2021, Infima Games. All Rights Reserved.

//  REFATORAÇÃO: scripts de colete, vida, munição, etc, podem herdar dessa classe, evitando código repetido? Precisamos atualizar? analise necessaria..(playerarmor)

// equippedWeapon veririca se estamos segurando buildables? ou medkit, grenade?

// Falta importar dentro de fields algum serviço como buildingcontroller?

// Precisamos colocar o hitmarker herdando daqui

using UnityEngine;

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

        #endregion

        #region UNITY

        protected virtual void Awake() {
            gameModeService = ServiceLocator.Current.Get<IGameModeService>();

            // CONCEITO: Null checks are essential. Services may not be initialized yet.
            // If gameModeService is null, the canvas spawned too early (before game mode ready).
            if (gameModeService == null) {
                Debug.LogWarning("[Element] IGameModeService not found. Canvas may have spawned before initialization.", gameObject);
                return;
            }

            playerCharacter = gameModeService.GetPlayerCharacter();
            
            // Additional safety check
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