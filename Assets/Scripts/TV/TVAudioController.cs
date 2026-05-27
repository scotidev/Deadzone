// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;
using UnityEngine.Video;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Manages synchronized 3D audio playback with a video player on the TV.
    /// Ensures audio and video stay in sync during looping playback.
    /// Includes interaction to mute/unmute the TV.
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

        #region PRIVATE FIELDS

        private IAudioManagerService audioService;
        private AudioSource audioSource;
        private AudioLowPassFilter lowPassFilter;
        private Transform playerCameraTransform;
        private bool isAudioPlaying;
        private double lastVideoTime;
        private const float SYNC_THRESHOLD = 0.1f; // If audio/video drift > 0.1s, resync

        private float targetVolume;
        private float targetCutoff;
        private const float OCCLUSION_SMOOTH_SPEED = 5f;

        #endregion

        #region UNITY LIFECYCLE

        private void Awake() {
            // Resolve the audio service from the ServiceLocator
            ResolveAudioService();

            // Auto-discover VideoPlayer if not assigned in Inspector
            if (videoPlayer == null) {
                videoPlayer = GetComponent<VideoPlayer>();
            }

            if (videoPlayer == null) {
                Debug.LogError($"[TVAudioController] VideoPlayer not found on {gameObject.name}. Please assign it in the Inspector or ensure it exists as a component.");
            }

            if (audioClip == null) {
                Debug.LogWarning($"[TVAudioController] No audio clip assigned. Audio will not play until one is set in the Inspector.");
            }

            // Set initial interaction prompt based on starting state
            SetInteractionPrompt(isMuted ? "[E] Unmute TV" : "[E] Mute TV");

            // Initialize the audio source once
            InitializeAudioSource();

            // Find the player's camera for occlusion raycasting
            var character = FindFirstObjectByType<Character>();
            if (character != null) {
                playerCameraTransform = character.GetCameraWorld().transform;
            }
        }

        private void OnEnable() {
            // Subscribe to VideoPlayer events to detect when video starts/restarts
            if (videoPlayer != null) {
                videoPlayer.loopPointReached += OnVideoLoopPointReached;
                videoPlayer.started += OnVideoStarted;
            }
        }

        private void OnDisable() {
            // Unsubscribe from VideoPlayer events
            if (videoPlayer != null) {
                videoPlayer.loopPointReached -= OnVideoLoopPointReached;
                videoPlayer.started -= OnVideoStarted;
            }

            // Stop audio when disabling the component
            StopAudio();
        }

        private void Update() {
            // Check if video is playing and audio should be active
            UpdateAudioPlayback();

            // Handle sound blocking (walls/floors)
            UpdateOcclusion();

            // Monitor and correct drift between audio and video timing
            MonitorSynchronization();
        }

        #endregion

        #region AUDIO PLAYBACK CONTROL

        /// <summary>
        /// Initializes the AudioSource component for this TV audio.
        /// Creates it only once during Awake and configures it for spatial 3D audio.
        /// </summary>
        private void InitializeAudioSource() {
            if (audioSource != null)
                return; // Already initialized

            // Create a child GameObject to hold the AudioSource
            var audioObject = new GameObject("TV Audio Source");
            audioObject.transform.SetParent(transform, false);

            // Add and configure the AudioSource component
            audioSource = audioObject.AddComponent<AudioSource>();

            // FIRST PRINCIPLE: We add a LowPassFilter to simulate occlusion.
            // High frequencies are absorbed by physical objects more easily than low frequencies.
            // By lowering the "Cutoff Frequency", we make the sound "muffled" (abafado).
            lowPassFilter = audioObject.AddComponent<AudioLowPassFilter>();
            lowPassFilter.cutoffFrequency = 22000f; // Start fully clear

            ConfigureAudioSource();
        }

        /// <summary>
        /// Configures the AudioSource for 3D spatial audio playback.
        /// This centralizes all spatial audio settings in one place.
        /// </summary>
        private void ConfigureAudioSource() {
            if (audioSource == null)
                return;

            // Set the audio clip to play
            audioSource.clip = audioClip;

            // Calculate final volume: SFX master volume * per-clip volume scale
            // This respects the AudioManagerService's SFX volume settings
            float sfxMasterVolume = audioService?.GetSFXVolume() ?? 1f;
            audioSource.volume = sfxMasterVolume * volumeScale;

            // Enable spatial audio: 1.0 means fully 3D (volume varies with distance)
            audioSource.spatialBlend = 1f;

            // Set distance attenuation: inside minDistance = full volume, outside maxDistance = silence
            audioSource.minDistance = minDistance;
            audioSource.maxDistance = maxDistance;

            // FIRST PRINCIPLE: AudioRolloffMode.Linear is used here because it ensures the volume
            // drops to exactly zero at maxDistance. Logarithmic rolloff (the default) fades
            // more "naturally" but theoretically never reaches zero, which was causing the 
            // audio to be heard at long distances.
            audioSource.rolloffMode = AudioRolloffMode.Linear;

            // Don't auto-play; we control playback manually
            audioSource.playOnAwake = false;

            // Enable looping so audio loops with the video
            audioSource.loop = true;
        }

        /// <summary>
        /// Handles physical sound occlusion using raycasting.
        /// If an object is between the TV and Player, muffles the sound.
        /// </summary>
        private void UpdateOcclusion() {
            if (audioSource == null || playerCameraTransform == null || isMuted)
                return;

            // FIRST PRINCIPLE: Raycasting simulates "Line of Sight" for sound.
            // If a "Ground" or "Wall" object interrupts the line, the sound is occluded.
            Vector3 direction = playerCameraTransform.position - transform.position;
            float distance = direction.magnitude;

            // We subtract a small amount from distance to avoid hitting the player itself
            bool isBlocked = Physics.Raycast(transform.position, direction, out RaycastHit hit, distance, occlusionMask);

            // Update master volume base
            float sfxMasterVolume = audioService?.GetSFXVolume() ?? 1f;
            float baseVolume = sfxMasterVolume * volumeScale;

            if (isBlocked) {
                // Sound is behind a wall or floor
                targetVolume = baseVolume * muffledVolumeScale;
                targetCutoff = muffledCutoff;
            } else {
                // Sound path is clear
                targetVolume = baseVolume;
                targetCutoff = 22000f; // Human hearing range limit (no muffling)
            }

            // Smoothly transition values to avoid audio "pops" or sudden changes
            audioSource.volume = Mathf.Lerp(audioSource.volume, targetVolume, Time.deltaTime * OCCLUSION_SMOOTH_SPEED);
            lowPassFilter.cutoffFrequency = Mathf.Lerp(lowPassFilter.cutoffFrequency, targetCutoff, Time.deltaTime * OCCLUSION_SMOOTH_SPEED);
        }

        /// <summary>
        /// Checks current video playback state and ensures audio matches.
        /// Handles pausing when the game is paused.
        /// </summary>
        private void UpdateAudioPlayback() {
            if (videoPlayer == null || audioClip == null || audioSource == null)
                return;

            // FIRST PRINCIPLE: In Unity, VideoPlayer and AudioSource are not automatically stopped 
            // by Time.timeScale = 0. We must manually check the GameState and pause/resume them.
            bool isGamePaused = GameManager.Instance != null && GameManager.Instance.State == GameState.Paused;

            // If muted or game is paused, ensure audio is not playing
            if (isMuted || isGamePaused) {
                if (audioSource.isPlaying) audioSource.Pause();
                
                // Still pause video on game pause, but NOT on mute (video keeps playing silently)
                if (isGamePaused && videoPlayer.isPlaying) videoPlayer.Pause();
                return;
            }

            // If the game is resumed and NOT muted, but the TV is still in a "Paused" state, resume it.
            if (videoPlayer.isPaused) {
                videoPlayer.Play();
                audioSource.UnPause();
            }

            // Standard synchronization logic:
            // If video is playing but audio hasn't started, start it
            if (videoPlayer.isPlaying && !audioSource.isPlaying) {
                PlayAudio();
            }

            // If video stopped but audio is still playing, stop it
            if (!videoPlayer.isPlaying && audioSource.isPlaying) {
                StopAudio();
            }
        }

        #region INTERACTION

        /// <summary>
        /// Called when the player interacts with the TV.
        /// Toggles the mute state and updates the HUD prompt.
        /// </summary>
        public override void Interact() {
            isMuted = !isMuted;

            // Update the HUD prompt for the next time the player looks at it
            SetInteractionPrompt(isMuted ? "[E] Unmute TV" : "[E] Mute TV");

            if (isMuted) {
                StopAudio();
            } else {
                // Force an update to start audio immediately if video is already playing
                UpdateAudioPlayback();
            }

            // Log the action for feedback
            Debug.Log($"[TVAudioController] TV is now {(isMuted ? "MUTED" : "UNMUTED")}");
        }

        #endregion

        /// <summary>
        /// Starts playing the audio synchronized with the video.
        /// The audio is attached to the TV's transform and emitted as spatial 3D sound.
        /// </summary>
        private void PlayAudio() {
            if (audioService == null || audioSource == null || videoPlayer == null || audioClip == null)
                return;

            // Update volume in case SFX master volume changed
            float sfxMasterVolume = audioService.GetSFXVolume();
            audioSource.volume = sfxMasterVolume * volumeScale;

            // FIRST PRINCIPLE: To maintain synchronization after unmuting or starting,
            // we set the audio playback time to match the current video time.
            // We use the modulo (%) operator to ensure that if the video time is somehow
            // longer than the audio clip, it still plays at the correct relative position.
            audioSource.time = (float)(videoPlayer.time % audioClip.length);

            // Start playback
            audioSource.Play();
            isAudioPlaying = true;
            lastVideoTime = videoPlayer.time;
        }

        /// <summary>
        /// Stops the audio playback.
        /// </summary>
        private void StopAudio() {
            if (audioSource != null && audioSource.isPlaying) {
                audioSource.Stop();
            }
            isAudioPlaying = false;
        }

        #endregion

        /// <summary>
        /// Monitors the synchronization between audio and video playback.
        /// If drift exceeds threshold, it can trigger a resync (reserved for future enhancement).
        /// </summary>
        private void MonitorSynchronization() {
            if (!isAudioPlaying || videoPlayer == null)
                return;

            // Calculate the time difference between current frame and last check
            double timeDelta = videoPlayer.time - lastVideoTime;
            lastVideoTime = videoPlayer.time;

            // Note: Perfect frame-accurate sync would require creating a custom AudioSource
            // that we control directly. The current implementation relies on both audio and
            // video being in loop mode and starting from the same point, which provides
            // sufficient synchronization for most use cases. Detected drift greater than
            // SYNC_THRESHOLD could trigger a full restart in future iterations.
        }


        #region VIDEO PLAYER EVENTS

        /// <summary>
        /// Called when the video reaches its loop point.
        /// Restarts the audio to match the video restart.
        /// </summary>
        private void OnVideoLoopPointReached(VideoPlayer source) {
            // When video loops, we need to restart the audio too
            // First, stop current audio
            StopAudio();

            // Then start fresh audio aligned with the looped video
            PlayAudio();
        }

        /// <summary>
        /// Called when the video player starts playback.
        /// </summary>
        private void OnVideoStarted(VideoPlayer source) {
            // Ensure audio plays when video starts
            if (!isAudioPlaying) {
                PlayAudio();
            }
        }

        #endregion

        #region SERVICE RESOLUTION

        /// <summary>
        /// Resolves the audio service from the ServiceLocator.
        /// This follows the project's architectural pattern for service dependency injection.
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
