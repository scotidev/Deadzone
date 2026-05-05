// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;
using UnityEngine.UI;

//FEATURE: como adicionar novas armas?

// REFATORAÇÃo: é mesmo necessario mostrar o pente, magazine, scope? talvez seja melhor mostrar apenas a arma, e não os detalhes, ou seja, o sprite da arma já com o pente, scope, etc...

namespace InfimaGames.LowPolyShooterPack.Interface {
    /// <summary>
    /// Weapon Image. Handles assigning the proper sprites to the weapon images.
    /// </summary>
    public class ImageWeapon : Element {

        #region SERIALIZED FIELDS

        [Header("Settings")]

        [SerializeField] private Image imageWeaponBody;
        [SerializeField] private Image imageWeaponMagazine;
        [SerializeField] private Image imageWeaponScopeDefault;

        #endregion

        #region FIELDS

        private WeaponAttachmentManagerBehaviour attachmentManagerBehaviour;

        #endregion

        #region METHODS

        /// <summary>
        /// Updates the element. Assigns the proper sprites to the weapon images.
        /// First principles: We verify that the weapon and its attachment manager are ready.
        /// </summary>
        protected override void Tick() {
            // Early exit if no weapon is equipped
            if (equippedWeapon == null) return;

            attachmentManagerBehaviour = equippedWeapon.GetAttachmentManager();
            
            // Safety check for attachment manager
            if (attachmentManagerBehaviour == null) return;

            imageWeaponBody.sprite = equippedWeapon.GetSpriteBody();

            Sprite sprite = default;

            ScopeBehaviour scopeDefaultBehaviour = attachmentManagerBehaviour.GetEquippedScopeDefault();

            if (scopeDefaultBehaviour != null)
                sprite = scopeDefaultBehaviour.GetSprite();

            AssignSprite(imageWeaponScopeDefault, sprite, scopeDefaultBehaviour == null);

            MagazineBehaviour magazineBehaviour = attachmentManagerBehaviour.GetEquippedMagazine();

            if (magazineBehaviour != null)
                sprite = magazineBehaviour.GetSprite();

            AssignSprite(imageWeaponMagazine, sprite, magazineBehaviour == null);
        }

        /// <summary>
        /// Assigns a sprite to an image.
        /// </summary>
        private static void AssignSprite(Image image, Sprite sprite, bool forceHide = false) {
            image.sprite = sprite;
            image.enabled = sprite != null && !forceHide;
        }

        #endregion
    }
}