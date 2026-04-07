// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Sound Manager Service Interface.
    /// Interface define um "contrato" que qualquer classe que a implemente deve seguir.
    /// Isso permite trocar implementações sem quebrar o código que usa a interface.
    /// </summary>
    public interface IAudioManagerService : IGameService
    {
        #region Legacy Methods (Manter compatibilidade com código existente)
        
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

        #region Background Music (BGM)
        
        /// <summary>
        /// Toca uma música de fundo (Background Music).
        /// BGM geralmente faz loop e persiste entre transições.
        /// </summary>
        /// <param name="clip">Clipe de música a tocar.</param>
        /// <param name="loop">Se true, a música vai repetir quando terminar.</param>
        /// <param name="fadeDuration">Tempo de fade in/out em segundos para transições suaves.</param>
        void PlayBGM(AudioClip clip, bool loop = true, float fadeDuration = 0f);
        
        /// <summary>
        /// Para a música de fundo atual.
        /// </summary>
        /// <param name="fadeDuration">Tempo de fade out em segundos.</param>
        void StopBGM(float fadeDuration = 0f);
        
        /// <summary>
        /// Define o volume geral da música de fundo (0 a 1).
        /// Isso afeta todas as músicas BGM, mas não afeta SFX.
        /// </summary>
        void SetBGMVolume(float volume);
        
        /// <summary>
        /// Obtém o volume atual da música de fundo.
        /// </summary>
        float GetBGMVolume();
        
        #endregion

        #region Sound Effects 2D (UI/HUD/Menu)
        
        /// <summary>
        /// Toca um efeito sonoro 2D (sem posição espacial).
        /// Usado para UI, HUD, menus - sons que não vêm de nenhum lugar específico no mundo.
        /// SpatialBlend = 0 significa som completamente 2D (mesmo volume em ambos os fones).
        /// </summary>
        /// <param name="clip">Clipe de som a tocar.</param>
        /// <param name="volumeScale">Multiplicador de volume para este som específico (0 a 1).</param>
        void PlaySFX2D(AudioClip clip, float volumeScale = 1f);
        
        /// <summary>
        /// Define o volume geral de efeitos sonoros (0 a 1).
        /// Afeta tanto SFX 2D quanto 3D, mas não afeta BGM.
        /// </summary>
        void SetSFXVolume(float volume);
        
        /// <summary>
        /// Obtém o volume atual de efeitos sonoros.
        /// </summary>
        float GetSFXVolume();
        
        #endregion

        #region Sound Effects 3D (World/Gameplay)
        
        /// <summary>
        /// Toca um efeito sonoro 3D em uma posição específica no mundo.
        /// SpatialBlend = 1 significa som completamente 3D (varia com distância e posição).
        /// O som fica mais alto quando o jogador está perto, mais baixo quando está longe.
        /// </summary>
        /// <param name="clip">Clipe de som a tocar.</param>
        /// <param name="position">Posição no mundo 3D onde o som será tocado.</param>
        /// <param name="volumeScale">Multiplicador de volume para este som específico (0 a 1).</param>
        /// <param name="minDistance">Distância mínima onde o som está no volume máximo.</param>
        /// <param name="maxDistance">Distância máxima onde o som ainda pode ser ouvido.</param>
        void PlaySFX3D(AudioClip clip, Vector3 position, float volumeScale = 1f, float minDistance = 1f, float maxDistance = 500f);
        
        /// <summary>
        /// Toca um efeito sonoro 3D que segue um Transform (útil para sons contínuos em objetos em movimento).
        /// Se o objeto se mover, o som vai se mover junto.
        /// </summary>
        /// <param name="clip">Clipe de som a tocar.</param>
        /// <param name="sourceTransform">Transform do objeto que emite o som.</param>
        /// <param name="volumeScale">Multiplicador de volume para este som específico (0 a 1).</param>
        /// <param name="minDistance">Distância mínima onde o som está no volume máximo.</param>
        /// <param name="maxDistance">Distância máxima onde o som ainda pode ser ouvido.</param>
        void PlaySFX3DAttached(AudioClip clip, Transform sourceTransform, float volumeScale = 1f, float minDistance = 1f, float maxDistance = 500f);
        
        #endregion
    }
}