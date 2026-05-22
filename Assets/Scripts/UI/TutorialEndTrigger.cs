using InfimaGames.LowPolyShooterPack;
    using UnityEngine;

    namespace Deadzone.UI {

        /// <summary>
        /// Trigger that ends the tutorial and starts the first official wave.
        /// Activates the poison system and triggers the WaveManager.
        /// </summary>
        public class TutorialEndTrigger : MonoBehaviour {

            #region SERIALIZED FIELDS

            [Header("Settings")]
            [Tooltip("If true, the trigger deactivates after the first entry.")]
            [SerializeField] private bool triggerOnce = true;

            #endregion

            #region UNITY

            /// <summary>
            /// Called when the player enters the trigger.
            /// Starts Wave 1 and enables game mechanics.
            /// </summary>
            private void OnTriggerEnter(Collider other) {
                // Verify if it is the player
                CharacterBehaviour character = other.GetComponentInParent<CharacterBehaviour>();
                if (character == null) return;

                // 1. Enable Poison/Fog logic in PlayerHealth
                PlayerHealth health = character.GetComponentInChildren<PlayerHealth>();
                if (health == null) health = character.GetComponent<PlayerHealth>();
                
                if (health != null) {
                    health.SetPoisonEnabled(true);
                    
                    // If the player is currently outside a safe zone when this trigger is hit, 
                    // we force the poison to start immediately.
                    // This handles the case where the player is already in a "poison area" but it was disabled.
                    // Note: SafeZone script will handle future enters/exits.
                    health.StartPoisonDamage();
                }

                // 2. Start Wave 1 immediately
                if (WaveManager.Instance != null && !WaveManager.Instance.IsWaveActive) {
                    WaveManager.Instance.StartNextWave();
                }

                // 3. Deactivate trigger
                if (triggerOnce) {
                    gameObject.SetActive(false);
                }
            }

            #endregion

        }

    }
    