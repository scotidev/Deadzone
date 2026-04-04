// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;
using System.Collections;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Manages the spawning and playing of sounds.
    /// Sistema unificado de áudio que gerencia BGM (música de fundo) e SFX (efeitos sonoros) em 2D e 3D.
    /// </summary>
    public class AudioManagerService : MonoBehaviour, IAudioManagerService
    {
        #region Background Music Variables
        
        /// <summary>
        /// AudioSource dedicado para Background Music (música de fundo).
        /// É um componente separado para permitir controle independente de volume e loops.
        /// </summary>
        private AudioSource bgmSource;
        
        /// <summary>
        /// Volume master da música de fundo (0 a 1).
        /// Todos os BGMs tocados usarão este volume como base.
        /// </summary>
        private float bgmVolume = 0.5f;
        
        #endregion

        #region Sound Effects Variables
        
        /// <summary>
        /// Volume master dos efeitos sonoros (0 a 1).
        /// Todos os SFX (2D e 3D) usarão este volume como base.
        /// </summary>
        private float sfxVolume = 1f;
        
        #endregion

        #region Unity Lifecycle
        
        /// <summary>
        /// Awake é chamado quando o script é carregado (antes de Start).
        /// Aqui criamos o AudioSource para BGM e configuramos volumes iniciais.
        /// </summary>
        private void Awake()
        {
            InitializeBGMSource();
        }
        
        /// <summary>
        /// Inicializa o AudioSource responsável pela música de fundo.
        /// Criamos dinamicamente em código para não depender de setup manual no Inspector.
        /// </summary>
        private void InitializeBGMSource()
        {
            // AddComponent<T>() adiciona um componente ao GameObject desta classe
            bgmSource = gameObject.AddComponent<AudioSource>();
            
            // SpatialBlend = 0 significa som 2D (mesmo volume em ambos os ouvidos)
            bgmSource.spatialBlend = 0f;
            
            // playOnAwake = false impede que toque automaticamente
            bgmSource.playOnAwake = false;
            
            // volume inicial
            bgmSource.volume = bgmVolume;
        }
        
        #endregion
        /// <summary>
        /// Contains data related to playing a OneShot audio.
        /// </summary>
        private readonly struct OneShotCoroutine
        {
            /// <summary>
            /// Audio Clip.
            /// </summary>
            public AudioClip Clip { get; }
            /// <summary>
            /// Audio Settings.
            /// </summary>
            public AudioSettings Settings { get; }
            /// <summary>
            /// Delay.
            /// </summary>
            public float Delay { get; }
            
            /// <summary>
            /// Constructor.
            /// </summary>
            public OneShotCoroutine(AudioClip clip, AudioSettings settings, float delay)
            {
                //Clip.
                Clip = clip;
                //Settings
                Settings = settings;
                //Delay.
                Delay = delay;
            }
        }

        /// <summary>
        /// Destroys the audio source once it has finished playing.
        /// </summary>
        private IEnumerator DestroySourceWhenFinished(AudioSource source)
        {
            //Wait for the audio source to complete playing the clip.
            yield return new WaitWhile(() => source.isPlaying);
            
            //Destroy the audio game object, since we're not using it anymore.
            //This isn't really too great for performance, but it works, for now.
            DestroyImmediate(source.gameObject);
        }

        /// <summary>
        /// Waits for a certain amount of time before starting to play a one shot sound.
        /// </summary>
        private IEnumerator PlayOneShotAfterDelay(OneShotCoroutine value)
        {
            //Wait for the delay.
            yield return new WaitForSeconds(value.Delay);
            //Play.
            PlayOneShot_Internal(value.Clip, value.Settings);
        }
        
        /// <summary>
        /// Internal PlayOneShot. Basically does the whole function's name!
        /// </summary>
        private void PlayOneShot_Internal(AudioClip clip, AudioSettings settings)
        {
            //No need to do absolutely anything if the clip is null.
            if (clip == null)
                return;
            
            //Spawn a game object for the audio source.
            var newSourceObject = new GameObject($"Audio Source -> {clip.name}");
            //Add an audio source component to that object.
            var newAudioSource = newSourceObject.AddComponent<AudioSource>();

            //Set volume.
            newAudioSource.volume = settings.Volume;
            //Set spatial blend.
            newAudioSource.spatialBlend = settings.SpatialBlend;
            
            //Play the clip!
            newAudioSource.PlayOneShot(clip);
            
            //Start a coroutine that will destroy the whole object once it is done!
            if(settings.AutomaticCleanup)
                StartCoroutine(nameof(DestroySourceWhenFinished), newAudioSource);
        }

        #region Audio Manager Service Interface - Legacy Methods
        
        /// <summary>
        /// Método legado mantido para compatibilidade com código existente.
        /// Usa o sistema interno de AudioSettings.
        /// </summary>
        public void PlayOneShot(AudioClip clip, AudioSettings settings = default)
        {
            PlayOneShot_Internal(clip, settings);
        }

        /// <summary>
        /// Método legado mantido para compatibilidade com código existente.
        /// Toca um som após um delay especificado.
        /// </summary>
        public void PlayOneShotDelayed(AudioClip clip, AudioSettings settings = default, float delay = 1.0f)
        {
            StartCoroutine(nameof(PlayOneShotAfterDelay), new OneShotCoroutine(clip, settings, delay));
        }

        #endregion

        #region Background Music Implementation
        
        /// <summary>
        /// Toca uma música de fundo (BGM).
        /// Se já houver música tocando, ela será substituída pela nova.
        /// </summary>
        public void PlayBGM(AudioClip clip, bool loop = true, float fadeDuration = 0f)
        {
            // Validação: não faz nada se o clip for nulo
            if (clip == null || bgmSource == null) return;
            
            // Se houver fade, usar coroutine para transição suave
            if (fadeDuration > 0f && bgmSource.isPlaying)
            {
                StartCoroutine(FadeBGM(clip, loop, fadeDuration));
                return;
            }
            
            // Sem fade: troca imediatamente
            bgmSource.clip = clip;
            bgmSource.loop = loop;
            bgmSource.Play();
        }
        
        /// <summary>
        /// Coroutine que faz fade out da música atual e fade in da nova.
        /// Coroutines permitem executar código ao longo de vários frames.
        /// </summary>
        private IEnumerator FadeBGM(AudioClip newClip, bool loop, float duration)
        {
            // Guarda o volume original para restaurar depois
            float startVolume = bgmSource.volume;
            float elapsed = 0f;
            
            // Fade out: reduz volume gradualmente até 0
            while (elapsed < duration / 2f)
            {
                elapsed += Time.deltaTime; // Time.deltaTime = tempo desde o último frame
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / (duration / 2f));
                yield return null; // Espera o próximo frame
            }
            
            // Troca a música no meio do fade
            bgmSource.Stop();
            bgmSource.clip = newClip;
            bgmSource.loop = loop;
            bgmSource.Play();
            
            elapsed = 0f;
            
            // Fade in: aumenta volume gradualmente até o volume original
            while (elapsed < duration / 2f)
            {
                elapsed += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(0f, startVolume, elapsed / (duration / 2f));
                yield return null;
            }
            
            // Garante que o volume final seja exato
            bgmSource.volume = startVolume;
        }
        
        /// <summary>
        /// Para a música de fundo atual.
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
        /// Coroutine que faz fade out e para a música.
        /// </summary>
        private IEnumerator FadeOutBGM(float duration)
        {
            float startVolume = bgmSource.volume;
            float elapsed = 0f;
            
            // Reduz volume gradualmente
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }
            
            bgmSource.Stop();
            bgmSource.volume = startVolume; // Restaura volume para próxima música
        }
        
        /// <summary>
        /// Define o volume master da música de fundo.
        /// </summary>
        public void SetBGMVolume(float volume)
        {
            // Mathf.Clamp01 garante que o valor fique entre 0 e 1
            bgmVolume = Mathf.Clamp01(volume);
            if (bgmSource != null)
                bgmSource.volume = bgmVolume;
        }
        
        /// <summary>
        /// Retorna o volume atual da música de fundo.
        /// </summary>
        public float GetBGMVolume()
        {
            return bgmVolume;
        }
        
        #endregion

        #region Sound Effects 2D Implementation
        
        /// <summary>
        /// Toca um efeito sonoro 2D (sem posição espacial).
        /// Ideal para UI, menus, HUD - sons que não têm origem no mundo 3D.
        /// </summary>
        public void PlaySFX2D(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null) return;
            
            // Cria um AudioSettings configurado para som 2D
            // spatialBlend = 0f significa som completamente 2D
            var settings = new AudioSettings(
                volume: sfxVolume * volumeScale, // Volume final = master * escala
                spatialBlend: 0f, // 2D
                automaticCleanup: true // Remove o AudioSource após tocar
            );
            
            PlayOneShot_Internal(clip, settings);
        }
        
        /// <summary>
        /// Define o volume master de efeitos sonoros.
        /// Afeta tanto SFX 2D quanto 3D.
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }
        
        /// <summary>
        /// Retorna o volume atual de efeitos sonoros.
        /// </summary>
        public float GetSFXVolume()
        {
            return sfxVolume;
        }
        
        #endregion

        #region Sound Effects 3D Implementation
        
        /// <summary>
        /// Toca um efeito sonoro 3D em uma posição específica no mundo.
        /// O som terá volume baseado na distância do ouvinte (câmera/jogador).
        /// </summary>
        public void PlaySFX3D(AudioClip clip, Vector3 position, float volumeScale = 1f, float minDistance = 1f, float maxDistance = 500f)
        {
            if (clip == null) return;
            
            // Cria um GameObject temporário na posição do som
            var audioObject = new GameObject($"SFX 3D -> {clip.name}");
            audioObject.transform.position = position;
            
            // Adiciona e configura o AudioSource
            var audioSource = audioObject.AddComponent<AudioSource>();
            ConfigureAudioSource3D(audioSource, clip, volumeScale, minDistance, maxDistance);
            
            // Toca o som
            audioSource.Play();
            
            // Inicia coroutine para destruir após terminar
            StartCoroutine(DestroySourceWhenFinished(audioSource));
        }
        
        /// <summary>
        /// Toca um efeito sonoro 3D que segue um Transform.
        /// Útil para sons contínuos ou sons de objetos em movimento.
        /// </summary>
        public void PlaySFX3DAttached(AudioClip clip, Transform sourceTransform, float volumeScale = 1f, float minDistance = 1f, float maxDistance = 500f)
        {
            if (clip == null || sourceTransform == null) return;
            
            // Cria AudioSource como filho do objeto especificado
            var audioObject = new GameObject($"SFX 3D Attached -> {clip.name}");
            
            // SetParent faz o audioObject seguir o sourceTransform
            // false = mantém posição local (0,0,0) relativa ao pai
            audioObject.transform.SetParent(sourceTransform, false);
            
            var audioSource = audioObject.AddComponent<AudioSource>();
            ConfigureAudioSource3D(audioSource, clip, volumeScale, minDistance, maxDistance);
            
            audioSource.Play();
            
            StartCoroutine(DestroySourceWhenFinished(audioSource));
        }
        
        /// <summary>
        /// Configura um AudioSource para som 3D espacial.
        /// Centraliza a configuração para evitar código duplicado.
        /// </summary>
        private void ConfigureAudioSource3D(AudioSource source, AudioClip clip, float volumeScale, float minDistance, float maxDistance)
        {
            source.clip = clip;
            source.volume = sfxVolume * volumeScale;
            
            // spatialBlend = 1f significa som completamente 3D
            source.spatialBlend = 1f;
            
            // minDistance: até essa distância, o som está no volume máximo
            source.minDistance = minDistance;
            
            // maxDistance: a partir dessa distância, o som não é mais audível
            source.maxDistance = maxDistance;
            
            // rolloffMode define como o volume diminui com a distância
            // Logarithmic é mais realista (volume cai rápido perto, devagar longe)
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            
            // playOnAwake deve ser false para controle manual
            source.playOnAwake = false;
        }
        
        #endregion
    }
}