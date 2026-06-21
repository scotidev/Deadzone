using System.Collections.Generic;
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

        [Tooltip("Objects (like zombies) to activate when the player enters this trigger.")]
        [SerializeField] private List<GameObject> objectsToActivate;

        [Header("Settings")]
        [Tooltip("If true, the trigger deactivates after the first entry.")]
        [SerializeField] private bool triggerOnce = true;

        #endregion

        #region UNITY

        /// <summary>
        /// Called when a collider enters the trigger. Detects the player, activates objects, and queues the tutorial.
        /// </summary>
        private void OnTriggerEnter(Collider other) {
            if (other.GetComponentInParent<CharacterBehaviour>() == null)
                return;

            if (objectsToActivate != null) {
                foreach (GameObject obj in objectsToActivate) {
                    if (obj == null) continue;

                    var follow = obj.GetComponent<EnemyFollow>();
                    if (follow != null) follow.enabled = true;

                    var attack = obj.GetComponent<EnemyAttack>();
                    if (attack != null) attack.enabled = true;
                }
            }

            if (tutorialStep != null) {
                TutorialManager.Instance?.QueueTutorial(tutorialStep);
            }

            if (triggerOnce)
                gameObject.SetActive(false);
        }

        #endregion

    }

}
