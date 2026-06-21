// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;
using UnityEngine.InputSystem;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Developer debug tool that allows real-time adjustment of Time.timeScale via keyboard input.
    /// All time scale writes are delegated to <see cref="GameManager"/>
    /// to respect the project's Single Source of Truth principle.
    /// </summary>
    public class TimeHandler : MonoBehaviour {

        #region SERIALIZED FIELDS

        [Header("Settings")]

        [Tooltip("Value the time scale gets updated by every time.")]
        [SerializeField] private float increment = 0.1f;

        #endregion

        #region FIELDS

        private bool paused;
        private float current = 1.0f;

        #endregion

        #region METHODS

        /// <summary>
        /// Applies the current time scale by delegating to <see cref="GameManager.SetTimeScale"/>.
        /// </summary>
        private void Scale() {
            GameManager.Instance?.SetTimeScale(current);
        }

        /// <summary>
        /// Saves the value as the new intended time scale and immediately applies it.
        /// </summary>
        /// <param name="value">The desired time multiplier (0..1).</param>
        private void Change(float value = 1.0f) {
            current = value;

            Scale();
        }

        /// <summary>
        /// Adds a value to the current time scale, clamping the result to the [0, 1] range.
        /// </summary>
        /// <param name="value">Amount to add. Positive = faster, negative = slower.</param>
        private void Increase(float value = 1.0f) {
            Change(Mathf.Clamp01(current + value));
        }

        /// <summary>
        /// Freezes time by delegating to <see cref="GameManager.PauseTime"/>.
        /// </summary>
        private void Pause() {
            paused = true;

            GameManager.Instance?.PauseTime();
        }

        /// <summary>
        /// Toggles between paused and unpaused states.
        /// </summary>
        private void Toggle() {
            if (paused)
                Unpause();
            else
                Pause();
        }

        /// <summary>
        /// Restores the time scale to the current saved value after a pause.
        /// </summary>
        private void Unpause() {
            paused = false;

            Change(current);
        }

        /// <summary>
        /// Input System callback that increases the time scale by the configured increment.
        /// </summary>
        /// <param name="context">Provides the action phase from the Input System.</param>
        public virtual void OnIncrease(InputAction.CallbackContext context) {
            switch (context.phase) {
                case InputActionPhase.Performed:
                    Increase(increment);
                    break;
            }
        }

        /// <summary>
        /// Input System callback that decreases the time scale by the configured increment.
        /// </summary>
        /// <param name="context">Provides the action phase from the Input System.</param>
        public virtual void OnDecrease(InputAction.CallbackContext context) {
            switch (context.phase) {
                case InputActionPhase.Performed:
                    Increase(-increment);
                    break;
            }
        }

        /// <summary>
        /// Input System callback that toggles time between frozen and the last active scale.
        /// </summary>
        /// <param name="context">Provides the action phase from the Input System.</param>
        public virtual void OnToggle(InputAction.CallbackContext context) {
            switch (context.phase) {
                case InputActionPhase.Performed:
                    Toggle();
                    break;
            }
        }

        #endregion
    }

}
