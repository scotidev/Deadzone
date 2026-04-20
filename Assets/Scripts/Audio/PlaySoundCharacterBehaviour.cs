// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Helper StateMachineBehaviour that allows us to more easily play a specific weapon sound.
    /// </summary>
    public class PlaySoundCharacterBehaviour : StateMachineBehaviour {

        #region ENUMS

        /// <summary>
        /// Type of weapon sound.
        /// </summary>
        private enum SoundType {
            Holster, Unholster,
            Reload, ReloadEmpty,
            Fire, FireEmpty,
        }

        #endregion

        #region FIELDS SERIALIZED

        [Header("Setup")]

        [SerializeField] private float delay;

        [Tooltip("Type of weapon sound to play.")]
        [SerializeField] private SoundType soundType;

        [Header("Audio Settings")]

        [SerializeField] private AudioSettings audioSettings = new AudioSettings(1.0f, 0.0f, true);

        #endregion

        #region FIELDS

        private CharacterBehaviour playerCharacter;
        private InventoryBehaviour playerInventory;
        private IAudioManagerService audioManagerService;

        #endregion

        #region UNITY

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            playerCharacter ??= ServiceLocator.Current.Get<IGameModeService>().GetPlayerCharacter();

            playerInventory ??= playerCharacter.GetInventory();

            if (!(playerInventory.GetEquipped() is { } weaponBehaviour))
                return;

            audioManagerService ??= ServiceLocator.Current.Get<IAudioManagerService>();

            AudioClip clip = soundType switch {
                SoundType.Holster => weaponBehaviour.GetAudioClipHolster(),
                SoundType.Unholster => weaponBehaviour.GetAudioClipUnholster(),

                SoundType.Reload => weaponBehaviour.GetAudioClipReload(),
                SoundType.ReloadEmpty => weaponBehaviour.GetAudioClipReloadEmpty(),

                SoundType.Fire => weaponBehaviour.GetAudioClipFire(),
                SoundType.FireEmpty => weaponBehaviour.GetAudioClipFireEmpty(),

                _ => default
            };

            audioManagerService.PlayOneShotDelayed(clip, audioSettings, delay);

            #endregion

        }
    }
}