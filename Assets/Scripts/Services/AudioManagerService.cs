// Copyright 2021, Infima Games. All Rights Reserved.

using System.Collections;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Manages the spawning and playing of sounds.
    /// Implements the IAudioManagerService interface, providing a centralized way to handle all audio playback in the game.
    /// </summary>
    public class AudioManagerService : MonoBehaviour, IAudioManagerService
    {

        #region FIELDS

        private AudioSource bgmSource;
        private AudioSource dialogueSource;
        private float bgmVolume = 0.5f;
        private float currentTrackVolume = 1f;
        private float sfxVolume = 1f;
        private float dialogueVolume = 1f;

        #endregion

        #region DATA STRUCTURES

        /// <summary>
        /// Contains data related to playing a OneShot audio.
        /// </summary>
        private readonly struct OneShotCoroutine
        {
            public AudioClip Clip { get; }
            public AudioSettings Settings { get; }
            public float Delay { get; }

            /// <summary>
            /// Constructor.
            /// </summary>
            public OneShotCoroutine(AudioClip clip, AudioSettings settings, float delay)
            {
                Clip = clip;
                Settings = settings;
                Delay = delay;
            }
        }

        #endregion

        #region UNITY

        private void Awake()
        {
            InitializeBGMSource();
            InitializeDialogueSource();
        }

        private void InitializeDialogueSource()
        {
            var dialogueObject = new GameObject("Dialogue Source");
            dialogueObject.transform.SetParent(transform);

            dialogueSource = dialogueObject.AddComponent<AudioSource>();
            dialogueSource.spatialBlend = 1f;
            dialogueSource.loop = false;
            dialogueSource.playOnAwake = false;
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Destroys the audio source once it has finished playing.
        /// </summary>
        private IEnumerator DestroySourceWhenFinished(AudioSource source)
        {
            yield return new WaitWhile(() => source != null && source.isPlaying);

            if (source != null && source.gameObject != null)
            {
                DestroyImmediate(source.gameObject);
            }
        }

        /// <summary>
        /// Waits for a certain amount of time before starting to play a one shot sound.
        /// </summary>
        private IEnumerator PlayOneShotAfterDelay(OneShotCoroutine value)
        {
            yield return new WaitForSeconds(value.Delay);
            PlayOneShot_Internal(value.Clip, value.Settings);
        }

        /// <summary>
        /// Internal PlayOneShot. Basically does the whole function's name!
        /// </summary>
        private void PlayOneShot_Internal(AudioClip clip, AudioSettings settings)
        {
            if (clip == null)
                return;

            var newSourceObject = new GameObject($"Audio Source -> {clip.name}");
            var newAudioSource = newSourceObject.AddComponent<AudioSource>();

            newAudioSource.volume = settings.Volume;
            newAudioSource.spatialBlend = settings.SpatialBlend;

            newAudioSource.PlayOneShot(clip);

            if (settings.AutomaticCleanup)
                StartCoroutine(nameof(DestroySourceWhenFinished), newAudioSource);
        }

        #region BGM

        /// <summary>
        /// Initalizes the AudioSource responsible for the background music.
        /// This avoids the need for a manual setup in the Inspector and ensures that the BGM source is always configured correctly when the AudioManager is created.
        /// </summary>
        private void InitializeBGMSource()
        {
            bgmSource = gameObject.AddComponent<AudioSource>();

            bgmSource.spatialBlend = 0f;

            bgmSource.playOnAwake = false;

            bgmSource.volume = bgmVolume * currentTrackVolume;
        }

        /// <summary>
        /// Plays a background music.
        /// If a BGM is already playing, it will be replaced by the new one.
        /// </summary>
        public void PlayBGM(AudioClip clip, bool loop = true, float fadeDuration = 0f, float volume = 1f)
        {
            if (clip == null || bgmSource == null) return;

            if (fadeDuration > 0f && bgmSource.isPlaying)
            {
                StartCoroutine(FadeBGM(clip, loop, fadeDuration, volume));
                return;
            }

            currentTrackVolume = Mathf.Clamp01(volume);
            bgmSource.clip = clip;
            bgmSource.loop = loop;
            bgmSource.volume = bgmVolume * currentTrackVolume;
            bgmSource.Play();
        }

        /// <summary>
        /// Coroutine that fades out the current music and fades in the new one.
        /// </summary>
        private IEnumerator FadeBGM(AudioClip newClip, bool loop, float duration, float trackVolume)
        {
            if (bgmSource == null) yield break;

            float startVolume = bgmSource.volume;
            float elapsed = 0f;

            while (elapsed < duration / 2f)
            {
                if (bgmSource == null) yield break;

                elapsed += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / (duration / 2f));
                yield return null;
            }

            if (bgmSource == null) yield break;

            currentTrackVolume = Mathf.Clamp01(trackVolume);
            float targetVolume = bgmVolume * currentTrackVolume;

            bgmSource.Stop();
            bgmSource.clip = newClip;
            bgmSource.loop = loop;
            bgmSource.Play();

            elapsed = 0f;

            while (elapsed < duration / 2f)
            {
                if (bgmSource == null) yield break;

                elapsed += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / (duration / 2f));
                yield return null;
            }

            if (bgmSource == null) yield break;

            bgmSource.volume = targetVolume;
        }

        /// <summary>
        /// Stops the current background music.
        /// </summary>
        public void StopBGM(float fadeDuration = 0f)
        {
            if (bgmSource == null) return;

            if (fadeDuration > 0f && bgmSource.isPlaying)
            {
                StartCoroutine(FadeOutBGM(fadeDuration));
            }
            else
            {
                bgmSource.Stop();
            }
        }

        /// <summary>
        /// Coroutine that fades out the current music and stops it.
        /// </summary>
        private IEnumerator FadeOutBGM(float duration)
        {
            if (bgmSource == null) yield break;

            float startVolume = bgmSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (bgmSource == null) yield break;

                elapsed += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }

            if (bgmSource == null) yield break;

            bgmSource.Stop();
            bgmSource.volume = startVolume;
        }

        /// <summary>
        /// Sets the master volume for background music.
        /// </summary>
        public void SetBGMVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            if (bgmSource != null)
                bgmSource.volume = bgmVolume * currentTrackVolume;
        }

        /// <summary>
        /// Returns the current background music volume.
        /// </summary>
        public float GetBGMVolume()
        {
            return bgmVolume;
        }

        #endregion

        #region SFX 2D

        /// <summary>
        /// Plays a 2D sound effect (non-spatial).
        /// Ideal for UI, menus, HUD - sounds that do not originate from the 3D world.
        /// <param name="clip">Audio clip to play.</param>
        /// <param name="volumeScale">Per-call volume scale.</param>
        /// </summary>
        public void PlaySFX2D(AudioClip clip, float volumeScale = 1f)
        {
            Play2DClip(clip, sfxVolume, volumeScale);
        }

        /// <summary>
        /// Plays a non-spatial dialogue clip.
        /// Dialogue is intentionally 2D so it remains clear regardless of world distance.
        /// <param name="clip">Audio clip to play.</param>
        /// <param name="volumeScale">Per-call volume scale.</param>
        /// </summary>
        public void PlayDialogue2D(AudioClip clip, float volumeScale = 1f)
        {
            Play2DClip(clip, dialogueVolume, volumeScale);
        }

        /// <summary>
        /// Shared 2D playback helper used by UI SFX and dialogue.
        /// </summary>
        /// <param name="clip">Clip to play.</param>
        /// <param name="masterVolume">Category master volume.</param>
        /// <param name="volumeScale">Per-call volume scale.</param>
        private void Play2DClip(AudioClip clip, float masterVolume, float volumeScale)
        {
            if (clip == null) return;

            float finalVolume = Mathf.Clamp01(masterVolume * volumeScale);

            var settings = new AudioSettings(
                volume: finalVolume,
                spatialBlend: 0f,
                automaticCleanup: true
            );

            PlayOneShot_Internal(clip, settings);
        }

        /// <summary>
        /// Sets the master volume for sound effects.
        /// Affects both 2D and 3D SFX.
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// Returns the current sound effects volume.
        /// </summary>
        public float GetSFXVolume()
        {
            return sfxVolume;
        }

        /// <summary>
        /// Sets the dialogue master volume.
        /// </summary>
        /// <param name="volume">Volume value in the [0, 1] range.</param>
        public void SetDialogueVolume(float volume)
        {
            dialogueVolume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// Gets the current dialogue master volume.
        /// </summary>
        /// <returns>Dialogue volume in the [0, 1] range.</returns>
        public float GetDialogueVolume()
        {
            return dialogueVolume;
        }

        #endregion

        #region SFX 3D

        /// <summary>
        /// Plays a 3D sound effect at a specific position in the world.
        /// The sound's volume will be based on the listener's distance (camera/player).
        /// </summary>
        public void PlaySFX3D(AudioClip clip, Vector3 position, float volumeScale = 1f, float minDistance = 1f, float maxDistance = 500f)
        {
            if (clip == null) return;

            var audioObject = new GameObject($"SFX 3D -> {clip.name}");
            audioObject.transform.position = position;

            var audioSource = audioObject.AddComponent<AudioSource>();
            ConfigureAudioSource3D(audioSource, clip, volumeScale, minDistance, maxDistance);

            audioSource.Play();

            StartCoroutine(DestroySourceWhenFinished(audioSource));
        }

        /// <summary>
        /// Plays a 3D sound effect that follows a Transform.
        /// Useful for continuous sounds or sounds from moving objects.
        /// </summary>
        public void PlaySFX3DAttached(AudioClip clip, Transform sourceTransform, float volumeScale = 1f, float minDistance = 1f, float maxDistance = 500f)
        {
            if (clip == null || sourceTransform == null) return;

            var audioObject = new GameObject($"SFX 3D Attached -> {clip.name}");

            audioObject.transform.SetParent(sourceTransform, false);

            var audioSource = audioObject.AddComponent<AudioSource>();
            ConfigureAudioSource3D(audioSource, clip, volumeScale, minDistance, maxDistance);

            audioSource.Play();

            StartCoroutine(DestroySourceWhenFinished(audioSource));
        }

        /// <summary>
        /// Plays a 3D dialogue sound, stopping any currently playing dialogue first.
        /// Used for merchant NPC dialogues that should be interrupted by new events.
        /// </summary>
        public void PlayDialogue3D(AudioClip clip, Vector3 position, float volumeScale = 1f, float minDistance = 1f, float maxDistance = 50f)
        {
            if (clip == null || dialogueSource == null) return;

            dialogueSource.Stop();

            dialogueSource.clip = clip;
            dialogueSource.transform.position = position;
            dialogueSource.spatialBlend = 1f;
            dialogueSource.minDistance = minDistance;
            dialogueSource.maxDistance = maxDistance;
            dialogueSource.volume = dialogueVolume * volumeScale;

            dialogueSource.Play();
        }

        public void PauseDialogue()
        {
            if (dialogueSource != null && dialogueSource.isPlaying)
            {
                dialogueSource.Pause();
            }
        }

        public void ResumeDialogue()
        {
            if (dialogueSource != null && !dialogueSource.isPlaying && dialogueSource.clip != null)
            {
                dialogueSource.UnPause();
            }
        }

        /// <summary>
        /// Configures an AudioSource for 3D spatial sound.
        /// Centralizes the configuration to avoid duplicate code.
        /// </summary>
        private void ConfigureAudioSource3D(AudioSource source, AudioClip clip, float volumeScale, float minDistance, float maxDistance)
        {
            source.clip = clip;
            source.volume = sfxVolume * volumeScale;

            source.spatialBlend = 1f;

            source.minDistance = minDistance;

            source.maxDistance = maxDistance;

            source.rolloffMode = AudioRolloffMode.Logarithmic;

            source.playOnAwake = false;
        }

        #endregion

        #region LEGACY

        /// <summary>
        /// Legacy method maintained for compatibility with existing code.
        /// Uses the internal AudioSettings system.
        /// </summary>
        public void PlayOneShot(AudioClip clip, AudioSettings settings = default)
        {
            PlayOneShot_Internal(clip, settings);
        }

        /// <summary>
        /// Legacy method maintained for compatibility with existing code.
        /// Plays a sound after a specified delay.
        /// </summary>
        public void PlayOneShotDelayed(AudioClip clip, AudioSettings settings = default, float delay = 1.0f)
        {
            StartCoroutine(nameof(PlayOneShotAfterDelay), new OneShotCoroutine(clip, settings, delay));
        }

        #endregion

        #endregion
    }
}
