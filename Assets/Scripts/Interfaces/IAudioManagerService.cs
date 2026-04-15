// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Sound Manager Service Interface.
    /// </summary>
    public interface IAudioManagerService : IGameService {
        #region Legacy Methods 

        /// <summary>
        /// Plays a one shot of the AudioClip.
        /// </summary>
        /// <param name="clip">Clip to play.</param>
        /// <param name="settings">Audio Settings.</param>
        void PlayOneShot(AudioClip clip, AudioSettings settings = default);

        /// <summary>
        /// Plays a one shot of the AudioClip, but waits for <paramref name="delay"/> before doing so.
        /// </summary>
        /// <param name="clip">Clip to play.</param>
        /// <param name="settings">Audio settings to use for this sound.</param>
        /// <param name="delay">Time to wait before we start playing this AudioClip.</param>
        void PlayOneShotDelayed(AudioClip clip, AudioSettings settings = default, float delay = 1.0f);

        #endregion

        #region BGM

        /// <summary>
        /// Plays a background music track (BGM).
        /// </summary>
        /// <param name="clip">Clip to play.</param>
        /// <param name="loop">If true, the music will loop when it finishes.</param>
        /// <param name="fadeDuration">Fade in/out duration in seconds for smooth transitions.</param>
        void PlayBGM(AudioClip clip, bool loop = true, float fadeDuration = 0f);

        /// <summary>
        /// Stops the current background music.
        /// </summary>
        /// <param name="fadeDuration">Fade out duration in seconds.</param>
        void StopBGM(float fadeDuration = 0f);

        /// <summary>
        /// Sets the master volume for background music (0 to 1).
        /// This affects all BGM tracks but does not affect SFX.
        /// </summary>
        void SetBGMVolume(float volume);

        /// <summary>
        /// Gets the current background music volume.
        /// </summary>
        float GetBGMVolume();

        #endregion

        #region SFX 2D

        /// <summary>
        /// Plays a 2D sound effect (non-spatial).
        /// Used for UI, HUD, menus - sounds that do not come from any specific location in the world.
        /// </summary>
        /// <param name="clip">Clip to play.</param>
        /// <param name="volumeScale">Per-clip volume multiplier (0 to 1).</param>
        void PlaySFX2D(AudioClip clip, float volumeScale = 1f);

        /// <summary>
        /// Plays a dialogue line as a non-spatial (2D) sound.
        /// Dialogue is intentionally 2D so it stays intelligible regardless of distance.
        /// </summary>
        /// <param name="clip">Dialogue clip to play.</param>
        /// <param name="volumeScale">Per-clip volume multiplier (0 to 1).</param>
        void PlayDialogue2D(AudioClip clip, float volumeScale = 1f);

        /// <summary>
        /// Sets the master volume for sound effects (0 to 1).
        /// This affects both 2D and 3D SFX but does not affect BGM.
        /// </summary>
        void SetSFXVolume(float volume);

        /// <summary>
        /// Gets the current master volume for sound effects.
        /// </summary>
        float GetSFXVolume();

        /// <summary>
        /// Sets the master volume for dialogue playback (0 to 1).
        /// </summary>
        void SetDialogueVolume(float volume);

        /// <summary>
        /// Gets the current dialogue master volume.
        /// </summary>
        float GetDialogueVolume();

        #endregion

        #region SFX 3D

        /// <summary>
        /// Plays a 3D sound effect at a specific position in the world.
        /// SpatialBlend = 1 means completely 3D sound (varies with distance and position).
        /// The sound is louder when the player is close, quieter when far away.
        /// </summary>
        /// <param name="clip">Clip to play.</param>
        /// <param name="position">Position in the 3D world where the sound will be played.</param>
        /// <param name="volumeScale">Per-clip volume multiplier (0 to 1).</param>
        /// <param name="minDistance">Minimum distance where the sound is at maximum volume.</param>
        /// <param name="maxDistance">Maximum distance where the sound can still be heard.</param>
        void PlaySFX3D(AudioClip clip, Vector3 position, float volumeScale = 1f, float minDistance = 1f, float maxDistance = 500f);

        /// <summary>
        /// Plays a 3D sound effect that follows a Transform (useful for continuous sounds on moving objects).
        /// If the object moves, the sound will move with it.
        /// </summary>
        /// <param name="clip">Clip to play.</param>
        /// <param name="sourceTransform">Transform of the object emitting the sound.</param>
        /// <param name="volumeScale">Per-clip volume multiplier (0 to 1).</param>
        /// <param name="minDistance">Minimum distance where the sound is at maximum volume.</param>
        /// <param name="maxDistance">Maximum distance where the sound can still be heard.</param>
        void PlaySFX3DAttached(AudioClip clip, Transform sourceTransform, float volumeScale = 1f, float minDistance = 1f, float maxDistance = 500f);

        #endregion
    }
}
