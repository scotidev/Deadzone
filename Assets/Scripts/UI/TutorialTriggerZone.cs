using InfimaGames.LowPolyShooterPack;
using UnityEngine;

namespace Deadzone.UI {

    /// <summary>
    /// Place this on a GameObject with a Collider (isTrigger = true).
    /// When the player enters the trigger, the assigned TutorialStepSO is queued.
    /// Useful for creating a tutorial phase with colliders placed before obstacles.
    /// </summary>
    public class TutorialTriggerZone : MonoBehaviour {

        #region SERIALIZED FIELDS

        [Header("References")]
        [Tooltip("Tutorial step to show when the player enters this trigger.")]
        [SerializeField] private TutorialStepSO tutorialStep;

        [Header("Settings")]
        [Tooltip("If true, the trigger deactivates after the first entry.")]
        [SerializeField] private bool triggerOnce = true;

        #endregion

        #region UNITY

        private void OnTriggerEnter(Collider other) {
            if (other.GetComponentInParent<CharacterBehaviour>() == null)
                return;

            TutorialManager.Instance?.QueueTutorial(tutorialStep);

            if (triggerOnce)
                gameObject.SetActive(false);
        }

        #endregion

    }

}
