using InfimaGames.LowPolyShooterPack;
    using UnityEngine;

    namespace Deadzone.UI {

        /// <summary>
        /// Trigger that ends the tutorial and starts the first official wave.
        /// Activates the poison system and triggers the WaveManager.
        /// Also transitions the camera far clip plane from tutorial to end-game distance.
        /// </summary>
        public class TutorialEndTrigger : MonoBehaviour {

            #region SERIALIZED FIELDS

            [Header("Settings")]
            [Tooltip("If true, the trigger deactivates after the first entry.")]
            [SerializeField] private bool triggerOnce = true;

            [Header("Camera Far Clip Plane")]
            [Tooltip("Far clip plane distance used during the tutorial (before crossing this trigger).")]
            [SerializeField] private float tutorialFarDistance = 15f;

            [Tooltip("Far clip plane distance used after the tutorial ends (after crossing this trigger).")]
            [SerializeField] private float endFarDistance = 200f;

            #endregion

            #region FIELDS

            private Camera playerCamera;

            #endregion

            #region UNITY

            /// <summary>
            /// Caches the player camera reference and sets the initial tutorial far clip distance.
            /// </summary>
            private void Start() {
                Character character = FindFirstObjectByType<Character>();
                if (character != null)
                {
                    playerCamera = character.GetCameraWorld();
                    if (playerCamera != null)
                    {
                        playerCamera.farClipPlane = tutorialFarDistance;
                    }
                }
            }

            /// <summary>
            /// Called when the player enters the trigger.
            /// Starts Wave 1, enables game mechanics, and transitions the camera far clip.
            /// </summary>
            private void OnTriggerEnter(Collider other) {
                CharacterBehaviour character = other.GetComponentInParent<CharacterBehaviour>();
                if (character == null) return;

                PlayerHealth health = character.GetComponentInChildren<PlayerHealth>();
                if (health == null) health = character.GetComponent<PlayerHealth>();

                if (health != null) {
                    health.SetPoisonEnabled(true);

                    SafeZone[] allSafeZones = FindObjectsByType<SafeZone>(FindObjectsSortMode.None);
                    foreach (SafeZone safeZone in allSafeZones) {
                        safeZone.EnablePoisonSystem();
                    }

                    Collider playerCollider = character.GetComponent<Collider>();
                    if (playerCollider != null) {
                        bool isInsideSafeZone = false;
                        foreach (SafeZone safeZone in allSafeZones) {
                            BoxCollider safeZoneBox = safeZone.GetComponent<BoxCollider>();
                            if (safeZoneBox != null && safeZoneBox.isTrigger) {
                                if (safeZoneBox.bounds.Intersects(playerCollider.bounds)) {
                                    isInsideSafeZone = true;
                                    break;
                                }
                            }
                        }

                        if (isInsideSafeZone) {
                            health.StopPoisonDamage();
                        } else {
                            health.StartPoisonDamage();
                        }
                    } else {
                        health.StartPoisonDamage();
                    }
                }

                FogController fog = FindFirstObjectByType<FogController>();
                if (fog != null) fog.EnableFog();

                if (playerCamera != null)
                {
                    playerCamera.farClipPlane = endFarDistance;
                }

                if (WaveManager.Instance != null && !WaveManager.Instance.IsWaveActive) {
                    WaveManager.Instance.StartNextWave();
                }

                if (triggerOnce) {
                    gameObject.SetActive(false);
                }
            }

            #endregion

        }

    }
    