// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Audio Settings struct used to interact with the AudioManagerService.
    /// </summary>
    [System.Serializable]
    public struct AudioSettings {

        #region SERIALIZED FIELDS

        [Header("Settings")]

        [Tooltip("If true, any AudioSource created will be removed after it has finished playing its clip.")]
        [SerializeField]
        private bool automaticCleanup;

        [Range(0.0f, 1.0f)]
        [SerializeField]
        private float volume;

        [Tooltip("Spatial Blend. If 0, the sound is fully 2D. If 1, the sound is fully 3D.")]
        [Range(0.0f, 1.0f)]
        [SerializeField]
        private float spatialBlend;

        #endregion

        #region PROPERTIES

        public bool AutomaticCleanup => automaticCleanup;
        public float Volume => volume;
        public float SpatialBlend => spatialBlend;

        #endregion

        #region METHODS

        public AudioSettings(float volume = 1.0f, float spatialBlend = 0.0f, bool automaticCleanup = true) {
            this.volume = volume;
            this.spatialBlend = spatialBlend;
            this.automaticCleanup = automaticCleanup;
        }

        #endregion

    }
}
