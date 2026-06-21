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
            ServiceLocator.Initialize();

            ServiceLocator.Current.Register<IGameModeService>(new GameModeService());

            #region Sound Manager Service

            var soundManagerObject = new GameObject("Sound Manager");
            var soundManagerService = soundManagerObject.AddComponent<AudioManagerService>();

            Object.DontDestroyOnLoad(soundManagerObject);

            ServiceLocator.Current.Register<IAudioManagerService>(soundManagerService);

            #endregion

            #region Game Manager

            var gameManagerObject = new GameObject("Game Manager");
            var gameManagerComponent = gameManagerObject.AddComponent<GameManager>();

            Object.DontDestroyOnLoad(gameManagerObject);

            #endregion

            #region Slow Motion Manager

            var slowMotionObject = new GameObject("Slow Motion Manager");
            var slowMotionComponent = slowMotionObject.AddComponent<SlowMotionManager>();

            Object.DontDestroyOnLoad(slowMotionObject);

            #endregion

            #region Player Progress

            var playerProgressObject = new GameObject("Player Progress");
            playerProgressObject.AddComponent<PlayerProgress>();

            Object.DontDestroyOnLoad(playerProgressObject);

            #endregion
        }
    }
}
