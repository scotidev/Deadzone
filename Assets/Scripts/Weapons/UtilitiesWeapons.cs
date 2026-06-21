using UnityEngine;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Weapon Static Utilities. Provides extension methods for weapon attachment arrays.
    /// </summary>
    public static class UtilitiesWeapons {
        /// <summary>
        /// Enables one object in the array at the given index, disables all others.
        /// </summary>
        public static T SelectAndSetActive<T>(this T[] array, int index) where T : MonoBehaviour {
            if (!array.IsValid())
                return null;

            array.ForEach(obj => obj.gameObject.SetActive(false));

            if (!array.IsValidIndex(index))
                return null;

            T behaviour = array[index];
            if (behaviour != null)
                behaviour.gameObject.SetActive(true);

            return behaviour;
        }
    }
}
