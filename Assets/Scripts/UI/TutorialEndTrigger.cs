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
                    
                    // 2. Enable poison logic in ALL SafeZones to begin tracking player position.
                    SafeZone[] allSafeZones = FindObjectsOfType<SafeZone>();
                    foreach (SafeZone safeZone in allSafeZones) {
                        safeZone.EnablePoisonSystem();
                    }
                    
                    // 3. Auto-detect if player is inside a SafeZone at tutorial end.
                    // Check if the player's collider bounds overlap with ANY SafeZone.
                    Collider playerCollider = character.GetComponent<Collider>();
                    if (playerCollider != null) {
                        bool isInsideSafeZone = false;
                        foreach (SafeZone safeZone in allSafeZones) {
                            BoxCollider safeZoneBox = safeZone.GetComponent<BoxCollider>();
                            if (safeZoneBox != null && safeZoneBox.isTrigger) {
                                // Use Intersects instead of Contains for better overlap detection.
                                if (safeZoneBox.bounds.Intersects(playerCollider.bounds)) {
                                    isInsideSafeZone = true;
                                    break;
                                }
                            }
                        }
                        
                        // Apply appropriate poison state based on detected position.
                        if (isInsideSafeZone) {
                            health.StopPoisonDamage();
                        } else {
                            health.StartPoisonDamage();
                        }
                    } else {
                        // Fallback: if no collider found, start poison damage (assume outside).
                        health.StartPoisonDamage();
                    }
                }

                // 4. Enable fog visual
                FogController fog = FindObjectOfType<FogController>();
                if (fog != null) fog.EnableFog();

                // 5. Start Wave 1 immediately
                if (WaveManager.Instance != null && !WaveManager.Instance.IsWaveActive) {
                    WaveManager.Instance.StartNextWave();
                }

                // 6. Deactivate trigger
                if (triggerOnce) {
                    gameObject.SetActive(false);
                }
            }

            #endregion

        }

    }
    