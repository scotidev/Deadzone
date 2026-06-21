using UnityEngine;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Plays an AudioClip using the centralized audio service when a state machine enters this state.
    /// </summary>
    public class PlaySoundBehaviour : StateMachineBehaviour {

        #region SERIALIZED FIELDS

        [Header("Setup")]
        [SerializeField] private AudioClip clip;

        [Header("Settings")]
        [SerializeField] private AudioSettings settings = new AudioSettings(1.0f, 0.0f, true);

        #endregion

        #region FIELDS

        private IAudioManagerService audioManagerService;

        #endregion

        #region UNITY

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            audioManagerService ??= ServiceLocator.Current.Get<IAudioManagerService>();

            audioManagerService?.PlayOneShot(clip, settings);
        }

        #endregion
    }
}
