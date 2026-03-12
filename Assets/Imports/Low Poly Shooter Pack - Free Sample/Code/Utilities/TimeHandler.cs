// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;
using UnityEngine.InputSystem;

// ==============================================================
//  ABOUT THIS FILE  (modified from original)
// ==============================================================
//  TimeHandler is a developer debug tool from the Low Poly Shooter
//  Pack that lets you change the game speed at runtime with
//  keyboard shortcuts.
//
//  ORIGINAL BEHAVIOUR: it wrote to Time.timeScale directly.
//
//  AFTER OUR REFACTOR: every time scale write is routed through
//  GameManager.SetTimeScale() and GameManager.PauseTime() so that
//  this tool respects the same Single Source of Truth rule as
//  every other system in the project.
//
//  Nothing outside this file needed to change — the public input
//  callbacks (OnIncrease, OnDecrease, OnToggle) still work the
//  same way when wired up via Unity's Input System.
// ==============================================================

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Developer debug tool that allows real-time adjustment of
    /// <c>Time.timeScale</c> via keyboard input.
    /// <para>
    /// All time scale writes are delegated to <see cref="GameManager"/>
    /// to respect the project's Single Source of Truth principle.
    /// </para>
    /// </summary>
    public class TimeHandler : MonoBehaviour
    {
        [Header("Settings")]

        [Tooltip("Value the time scale gets updated by every time.")]
        [SerializeField]
        private float increment = 0.1f;

        // ==============================================================
        //  INTERNAL STATE
        // ==============================================================
        //  "paused" tracks whether we have intentionally frozen time
        //  so that Toggle() knows which direction to go.
        //
        //  "current" stores the last non-zero time scale we asked for,
        //  so that Unpause() can restore it without hard-coding 1f.
        // ==============================================================

        /// <summary>
        /// Whether time has been manually stopped via <see cref="Pause"/>.
        /// </summary>
        private bool paused;

        /// <summary>
        /// The last time scale value requested while not paused.
        /// Restored when <see cref="Unpause"/> is called.
        /// </summary>
        private float current = 1.0f;

        // ==============================================================
        //  PRIVATE HELPERS
        // ==============================================================

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
        private void Scale()
        {
            // We no longer touch Time.timeScale here.
            // GameManager.SetTimeScale() handles both timeScale and
            // fixedDeltaTime in one atomic call, keeping physics correct.
            GameManager.Instance?.SetTimeScale(current);
        }

        /// <summary>
        /// Saves <paramref name="value"/> as the new intended time scale
        /// and immediately applies it via <see cref="Scale"/>.
        /// </summary>
        /// <param name="value">The desired time multiplier (0..1).</param>
        private void Change(float value = 1.0f)
        {
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
        private void Increase(float value = 1.0f)
        {
            // Mathf.Clamp01 keeps the result between 0 and 1,
            // preventing invalid time scale values.
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
        private void Pause()
        {
            //Pause.
            paused = true;

            // Route through GameManager instead of writing Time.timeScale = 0f
            // directly. GameManager.PauseTime() handles the slow motion
            // cancellation edge case and keeps fixedDeltaTime consistent.
            GameManager.Instance?.PauseTime();
        }

        /// <summary>
        /// Toggles between paused and unpaused states.
        /// </summary>
        private void Toggle()
        {
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
        private void Unpause()
        {
            //Unpause.
            paused = false;

            // Change() → Scale() → GameManager.SetTimeScale(current).
            // This restores both timeScale and fixedDeltaTime correctly.
            Change(current);
        }

        // ==============================================================
        //  INPUT SYSTEM CALLBACKS
        // ==============================================================
        //  These public methods are wired to Unity's Input System actions
        //  via the Inspector. They fire when the player presses the
        //  mapped keys and forward the work to the private helpers above.
        //
        //  InputActionPhase.Performed fires once per key press (not held),
        //  which is why we use a switch instead of a simple if-check.
        // ==============================================================

        /// <summary>
        /// Input System callback that increases the time scale by
        /// <see cref="increment"/> when the mapped key is pressed.
        /// </summary>
        /// <param name="context">Provides the action phase from the Input System.</param>
        public virtual void OnIncrease(InputAction.CallbackContext context)
        {
            //Switch.
            switch (context.phase)
            {
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
        public virtual void OnDecrease(InputAction.CallbackContext context)
        {
            //Switch.
            switch (context.phase)
            {
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
        public virtual void OnToggle(InputAction.CallbackContext context)
        {
            //Switch.
            switch (context.phase)
            {
                //Performed.
                case InputActionPhase.Performed:
                    //Toggle.
                    Toggle();
                    break;
            }
        }
    }
}