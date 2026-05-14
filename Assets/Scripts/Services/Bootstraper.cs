// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Bootstraper.
    /// </summary>
    public static class Bootstraper
    {
        /// <summary>
        /// Initialize.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            //Initialize default service locator.
            ServiceLocator.Initialize();
            
            //Game Mode Service.
            ServiceLocator.Current.Register<IGameModeService>(new GameModeService());
            
            #region Sound Manager Service

            //Create an object for the sound manager, and add the component!
            var soundManagerObject = new GameObject("Sound Manager");
            var soundManagerService = soundManagerObject.AddComponent<AudioManagerService>();
            
            //Make sure that we never destroy our SoundManager. We need it in other scenes too!
            Object.DontDestroyOnLoad(soundManagerObject);
            
            //Register the sound manager service!
            ServiceLocator.Current.Register<IAudioManagerService>(soundManagerService);

            #endregion

            #region Game Manager

            // Cria um objeto para o GameManager e adiciona o componente
            // Isso garante que o GameManager esteja disponível em qualquer scene
            var gameManagerObject = new GameObject("Game Manager");
            var gameManagerComponent = gameManagerObject.AddComponent<GameManager>();
            
            // Marca como DontDestroyOnLoad para persistir entre scenes
            Object.DontDestroyOnLoad(gameManagerObject);

            #endregion

            #region Slow Motion Manager

            // Cria o SlowMotionManager como facade para o sistema de slow motion
            // Permite que qualquer script acesse a funcionalidade de slow motion
            var slowMotionObject = new GameObject("Slow Motion Manager");
            var slowMotionComponent = slowMotionObject.AddComponent<SlowMotionManager>();
            
            // Marca como DontDestroyOnLoad para persistir entre scenes
            Object.DontDestroyOnLoad(slowMotionObject);

            #endregion

            // NOTA: SceneLoader não é mais criado aqui.
            // Ele deve ser colocado manualmente na cena Intro (ex-Loader)
            // com o prefab da Loading Screen já atribuído no Inspector.
            // O Awake() dele cuida do singleton + DontDestroyOnLoad.
        }
    }
}