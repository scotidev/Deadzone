// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;
using UnityEngine.Video;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Manages synchronized 3D audio playback with a video player on the TV.
    /// Handles mute/unmute interaction, occlusion through walls, and audio-video sync.
    /// </summary>
    [DisallowMultipleComponent]
    public class TVAudioController : Interactable {

        #region SERIALIZED FIELDS

        [Header("Audio Settings")]
        [SerializeField]
        [Tooltip("The MP3 audio clip to play synchronized with the video")]
        private AudioClip audioClip;

        [SerializeField]
        [Tooltip("Volume multiplier for this audio source (1.0 = normal, >1.0 = louder)")]
        private float volumeScale = 1f;

        [SerializeField]
        [Tooltip("If true, the TV starts without sound. Can be toggled by player interaction.")]
        private bool isMuted = true;

        [Header("3D Spatial Settings")]
        [SerializeField]
        [Tooltip("Minimum distance at which audio is at maximum volume")]
        private float minDistance = 1.5f;

        [SerializeField]
        [Tooltip("Maximum distance at which audio can be heard")]
        private float maxDistance = 2f;

        [Header("Occlusion Settings (Through Walls/Floors)")]
        [SerializeField]
        [Tooltip("Layers that block the TV sound (e.g., Ground, Wall)")]
        private LayerMask occlusionMask;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Volume multiplier when the sound is blocked (0.2 = 20% volume)")]
        private float muffledVolumeScale = 0.2f;

        [SerializeField]
        [Range(500f, 22000f)]
        [Tooltip("Frequency cutoff when blocked. Lower = more muffled/bassy sound.")]
        private float muffledCutoff = 1000f;

        [Header("References")]
        [SerializeField]
        [Tooltip("Reference to the VideoPlayer component on this GameObject or child")]
        private VideoPlayer videoPlayer;

        #endregion

        #region FIELDS

        private IAudioManagerService audioService;
        private AudioSource audioSource;
        private AudioLowPassFilter lowPassFilter;
        private Transform playerCameraTransform;
        private bool isAudioPlaying;
        private double lastVideoTime;
        private const float SYNC_THRESHOLD = 0.1f;

        private float targetVolume;
        private float targetCutoff;
        private const float OCCLUSION_SMOOTH_SPEED = 5f;

        #endregion

        #region UNITY

        private void Awake() {
            ResolveAudioService();

            if (videoPlayer == null) {
                videoPlayer = GetComponentInChildren<VideoPlayer>();
            }

            if (videoPlayer == null) {
                Debug.LogError($"[TVAudioController] VideoPlayer not found on {gameObject.name}. Please assign it in the Inspector or ensure it exists as a component.");
            }
            else
            {
                videoPlayer.source = VideoSource.Url;
                videoPlayer.url = System.IO.Path.Combine(Application.streamingAssetsPath, "TeenTitans.mp4");
                videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
                videoPlayer.isLooping = true;
                videoPlayer.playOnAwake = true;
            }

            if (audioClip == null) {
                Debug.LogWarning($"[TVAudioController] No audio clip assigned. Audio will not play until one is set in the Inspector.");
            }

            SetInteractionPrompt(isMuted ? "[E] Unmute TV" : "[E] Mute TV");

            InitializeAudioSource();

            var character = FindFirstObjectByType<Character>();
            if (character != null) {
                playerCameraTransform = character.GetCameraWorld().transform;
            }
        }

        private void OnEnable() {
            if (videoPlayer != null) {
                videoPlayer.loopPointReached += OnVideoLoopPointReached;
                videoPlayer.started += OnVideoStarted;
            }
        }

        private void OnDisable() {
            if (videoPlayer != null) {
                videoPlayer.loopPointReached -= OnVideoLoopPointReached;
                videoPlayer.started -= OnVideoStarted;
            }

            StopAudio();
        }

        private void Update() {
            UpdateAudioPlayback();
            UpdateOcclusion();
            MonitorSynchronization();
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Creates and configures the AudioSource component for spatial 3D audio playback.
        /// </summary>
        private void InitializeAudioSource() {
            if (audioSource != null)
                return;

            var audioObject = new GameObject("TV Audio Source");
            audioObject.transform.SetParent(transform, false);

            audioSource = audioObject.AddComponent<AudioSource>();

            lowPassFilter = audioObject.AddComponent<AudioLowPassFilter>();
            lowPassFilter.cutoffFrequency = 22000f;

            ConfigureAudioSource();
        }

        /// <summary>
        /// Configures the AudioSource for 3D spatial audio with distance attenuation and looping.
        /// </summary>
        private void ConfigureAudioSource() {
            if (audioSource == null)
                return;

            audioSource.clip = audioClip;

            float sfxMasterVolume = audioService?.GetSFXVolume() ?? 1f;
            audioSource.volume = sfxMasterVolume * volumeScale;

            audioSource.spatialBlend = 1f;

            audioSource.minDistance = minDistance;
            audioSource.maxDistance = maxDistance;

            audioSource.rolloffMode = AudioRolloffMode.Linear;

            audioSource.playOnAwake = false;

            audioSource.loop = true;
        }

        /// <summary>
        /// Uses raycasting to detect obstacles between the TV and the player, applying a muffled effect when sound is occluded.
        /// </summary>
        private void UpdateOcclusion() {
            if (audioSource == null || playerCameraTransform == null || isMuted)
                return;

            Vector3 direction = playerCameraTransform.position - transform.position;
            float distance = direction.magnitude;

            bool isBlocked = Physics.Raycast(transform.position, direction, out RaycastHit hit, distance, occlusionMask);

            float sfxMasterVolume = audioService?.GetSFXVolume() ?? 1f;
            float baseVolume = sfxMasterVolume * volumeScale;

            if (isBlocked) {
                targetVolume = baseVolume * muffledVolumeScale;
                targetCutoff = muffledCutoff;
            } else {
                targetVolume = baseVolume;
                targetCutoff = 22000f;
            }

            audioSource.volume = Mathf.Lerp(audioSource.volume, targetVolume, Time.deltaTime * OCCLUSION_SMOOTH_SPEED);
            lowPassFilter.cutoffFrequency = Mathf.Lerp(lowPassFilter.cutoffFrequency, targetCutoff, Time.deltaTime * OCCLUSION_SMOOTH_SPEED);
        }

        /// <summary>
        /// Synchronizes audio playback with the video player state, handling pause/resume and mute.
        /// </summary>
        private void UpdateAudioPlayback() {
            if (videoPlayer == null || audioClip == null || audioSource == null)
                return;

            bool isGamePaused = GameManager.Instance != null && GameManager.Instance.State == GameState.Paused;

            if (isMuted || isGamePaused) {
                if (audioSource.isPlaying) audioSource.Pause();

                if (isGamePaused && videoPlayer.isPlaying) videoPlayer.Pause();
                return;
            }

            if (videoPlayer.isPaused) {
                videoPlayer.Play();
                audioSource.UnPause();
            }

            if (videoPlayer.isPlaying && !audioSource.isPlaying) {
                PlayAudio();
            }

            if (!videoPlayer.isPlaying && audioSource.isPlaying) {
                StopAudio();
            }
        }

        /// <summary>
        /// Toggles the mute state when the player interacts with the TV.
        /// </summary>
        public override void Interact() {
            isMuted = !isMuted;

            SetInteractionPrompt(isMuted ? "[E] Unmute TV" : "[E] Mute TV");

            if (isMuted) {
                StopAudio();
            } else {
                UpdateAudioPlayback();
            }
        }

        /// <summary>
        /// Starts audio playback synchronized with the current video time.
        /// </summary>
        private void PlayAudio() {
            if (audioService == null || audioSource == null || videoPlayer == null || audioClip == null)
                return;

            float sfxMasterVolume = audioService.GetSFXVolume();
            audioSource.volume = sfxMasterVolume * volumeScale;

            audioSource.time = (float)(videoPlayer.time % audioClip.length);

            audioSource.Play();
            isAudioPlaying = true;
            lastVideoTime = videoPlayer.time;
        }

        /// <summary>
        /// Stops audio playback.
        /// </summary>
        private void StopAudio() {
            if (audioSource != null && audioSource.isPlaying) {
                audioSource.Stop();
            }
            isAudioPlaying = false;
        }

        /// <summary>
        /// Monitors audio-video synchronization by tracking time drift. Reserved for future resync logic.
        /// </summary>
        private void MonitorSynchronization() {
            if (!isAudioPlaying || videoPlayer == null)
                return;

            double timeDelta = videoPlayer.time - lastVideoTime;
            lastVideoTime = videoPlayer.time;
        }

        #endregion

        #region VIDEO PLAYER EVENTS

        /// <summary>
        /// Called when the video reaches its loop point. Restarts audio to match.
        /// </summary>
        private void OnVideoLoopPointReached(VideoPlayer source) {
            StopAudio();
            PlayAudio();
        }

        /// <summary>
        /// Called when the video player starts playback. Ensures audio starts as well.
        /// </summary>
        private void OnVideoStarted(VideoPlayer source) {
            if (!isAudioPlaying) {
                PlayAudio();
            }
        }

        #endregion

        #region SERVICE RESOLUTION

        /// <summary>
        /// Resolves the audio service from the ServiceLocator.
        /// </summary>
        private void ResolveAudioService() {
            audioService = ServiceLocator.Current.Get<IAudioManagerService>();

            if (audioService == null) {
                Debug.LogError($"[TVAudioController] Failed to resolve IAudioManagerService from ServiceLocator. Ensure AudioManagerService is initialized.");
            }
        }

        #endregion
    }
}
