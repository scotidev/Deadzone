namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Game Mode Service.
    /// </summary>
    public class GameModeService : IGameModeService {
        #region FIELDS

        /// <summary>
        /// The Player Character.
        /// </summary>
        private CharacterBehaviour playerCharacter;

        #endregion

        #region FUNCTIONS

        /// <summary>
        /// Returns the Player Character, finding it if not yet cached.
        /// </summary>
        public CharacterBehaviour GetPlayerCharacter() {
            if (playerCharacter == null)
                playerCharacter = UnityEngine.Object.FindFirstObjectByType<CharacterBehaviour>();

            return playerCharacter;
        }

        #endregion
    }
}
