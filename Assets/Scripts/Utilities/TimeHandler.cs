// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;
using UnityEngine.InputSystem;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Developer debug tool that allows real-time adjustment of
    /// <c>Time.timeScale</c> via keyboard input.
    /// <para>
    /// All time scale writes are delegated to <see cref="GameManager"/>
    /// to respect the project's Single Source of Truth principle.
    /// </para>
    /// </summary>
    public class TimeHandler : MonoBehaviour {
        [Header("Settings")]

        [Tooltip("Value the time scale gets updated by every time.")]
        [SerializeField]
        private float increment = 0.1f;

        /// <summary>
        /// Whether time has been manually stopped via <see cref="Pause"/>.
        /// </summary>
        private bool paused;

        /// <summary>
        /// The last time scale value requested while not paused.
        /// Restored when <see cref="Unpause"/> is called.
        /// </summary>
        private float current = 1.0f;

        /// <summary>
        /// Applies <see cref="current"/> as the active time scale by
        /// delegating to <see cref="GameManager.SetTimeScale"/>.
        /// <para>
        /// <b>Modified:</b> previously wrote <c>Time.timeScale = current</c>
        /// directly. Now routes through GameManager so the single source
        /// of truth rule is respected and <c>fixedDeltaTime</c> is also
        /// updated automatically.
        /// </para>
        /// </summary>
        private void Scale() {
            GameManager.Instance?.SetTimeScale(current);
        }

        /// <summary>
        /// Saves <paramref name="value"/> as the new intended time scale
        /// and immediately applies it via <see cref="Scale"/>.
        /// </summary>
        /// <param name="value">The desired time multiplier (0..1).</param>
        private void Change(float value = 1.0f) {
            //Save Value.
            current = value;

            //Update.
            Scale();
        }

        /// <summary>
        /// Adds <paramref name="value"/> to the current time scale,
        /// clamping the result to the [0, 1] range.
        /// </summary>
        /// <param name="value">
        /// Amount to add. Positive = faster, negative = slower.
        /// </param>
        private void Increase(float value = 1.0f) {
            Change(Mathf.Clamp01(current + value));
        }

        /// <summary>
        /// Freezes time by delegating to <see cref="GameManager.PauseTime"/>.
        /// <para>
        /// <b>Modified:</b> previously set <c>Time.timeScale = 0f</c> directly.
        /// Now calls <c>GameManager.PauseTime()</c>, which also cancels any
        /// active slow motion coroutine before freezing — preventing the
        /// coroutine from unfreezing the game when its timer expires.
        /// </para>
        /// </summary>
        private void Pause() {
            paused = true;

            GameManager.Instance?.PauseTime();
        }

        /// <summary>
        /// Toggles between paused and unpaused states.
        /// </summary>
        private void Toggle() {
            //Toggle Pause.
            if (paused)
                Unpause();
            else
                Pause();
        }

        /// <summary>
        /// Restores the time scale to <see cref="current"/> after a pause.
        /// Calls <see cref="Change"/> which routes through <see cref="Scale"/>
        /// and therefore through <see cref="GameManager.SetTimeScale"/>.
        /// </summary>
        private void Unpause() {
            //Unpause.
            paused = false;

            // Change() → Scale() → GameManager.SetTimeScale(current).
            // This restores both timeScale and fixedDeltaTime correctly.
            Change(current);
        }

        /// <summary>
        /// Input System callback that increases the time scale by
        /// <see cref="increment"/> when the mapped key is pressed.
        /// </summary>
        /// <param name="context">Provides the action phase from the Input System.</param>
        public virtual void OnIncrease(InputAction.CallbackContext context) {
            //Switch.
            switch (context.phase) {
                //Performed.
                case InputActionPhase.Performed:
                    //Increase.
                    Increase(increment);
                    break;
            }
        }

        /// <summary>
        /// Input System callback that decreases the time scale by
        /// <see cref="increment"/> when the mapped key is pressed.
        /// </summary>
        /// <param name="context">Provides the action phase from the Input System.</param>
        public virtual void OnDecrease(InputAction.CallbackContext context) {
            //Switch.
            switch (context.phase) {
                //Performed.
                case InputActionPhase.Performed:
                    // Passing a negative increment effectively subtracts
                    // from the current time scale.
                    Increase(-increment);
                    break;
            }
        }

        /// <summary>
        /// Input System callback that toggles time between frozen and
        /// the last active scale when the mapped key is pressed.
        /// </summary>
        /// <param name="context">Provides the action phase from the Input System.</param>
        public virtual void OnToggle(InputAction.CallbackContext context) {
            //Switch.
            switch (context.phase) {
                //Performed.
                case InputActionPhase.Performed:
                    //Toggle.
                    Toggle();
                    break;
            }
        }
    }
}