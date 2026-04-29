// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

//REFATORAÇÃO: mudar por aqui a sensibilidade quando ela for atualizada no mehu la em OptionsUI

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Camera Look. Handles the rotation of the camera and player character when looking around.
    /// </summary>
    public class CameraLook : MonoBehaviour {

        #region SERIALIZED FIELDS

        [Header("Settings")]

        [SerializeField] private Vector2 sensitivity = new Vector2(1, 1);
        [SerializeField] private bool smooth;
        [SerializeField] private float interpolationSpeed = 25.0f;

        [Tooltip("Minimum and maximum up/down rotation angle the camera can have.")]
        [SerializeField] private Vector2 yClamp = new Vector2(-60, 60);

        #endregion

        #region FIELDS

        private CharacterBehaviour playerCharacter;
        private Rigidbody playerCharacterRigidbody;
        private Quaternion rotationCharacter;
        private Quaternion rotationCamera;

        #endregion

        #region UNITY

        private void Awake() {
            playerCharacter = ServiceLocator.Current.Get<IGameModeService>().GetPlayerCharacter();
            playerCharacterRigidbody = playerCharacter.GetComponent<Rigidbody>();
        }
        private void Start() {
            rotationCharacter = playerCharacter.transform.localRotation;
            rotationCamera = transform.localRotation;
        }
        private void LateUpdate() {
            Vector2 frameInput = playerCharacter.IsCursorLocked() ? playerCharacter.GetInputLook() : default;
            frameInput *= sensitivity;

            Quaternion rotationYaw = Quaternion.Euler(0.0f, frameInput.x, 0.0f);
            Quaternion rotationPitch = Quaternion.Euler(-frameInput.y, 0.0f, 0.0f);

            rotationCamera *= rotationPitch;
            rotationCharacter *= rotationYaw;

            Quaternion localRotation = transform.localRotation;

            if (smooth) {
                localRotation = Quaternion.Slerp(localRotation, rotationCamera, Time.deltaTime * interpolationSpeed);

                playerCharacterRigidbody.MoveRotation(Quaternion.Slerp(playerCharacterRigidbody.rotation, rotationCharacter, Time.deltaTime * interpolationSpeed));
            } else {
                localRotation *= rotationPitch;
                localRotation = Clamp(localRotation);

                playerCharacterRigidbody.MoveRotation(playerCharacterRigidbody.rotation * rotationYaw);
            }

            transform.localRotation = localRotation;
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Clamps the pitch of a quaternion according to our clamps.
        /// </summary>
        private Quaternion Clamp(Quaternion rotation) {
            rotation.x /= rotation.w;
            rotation.y /= rotation.w;
            rotation.z /= rotation.w;
            rotation.w = 1.0f;

            float pitch = 2.0f * Mathf.Rad2Deg * Mathf.Atan(rotation.x);

            pitch = Mathf.Clamp(pitch, yClamp.x, yClamp.y);
            rotation.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * pitch);

            return rotation;
        }

        #endregion
    }
}