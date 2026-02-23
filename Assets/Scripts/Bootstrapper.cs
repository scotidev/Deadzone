using UnityEngine;

/// <summary>
/// Entry point of the game. Attached to a GameObject in the Loader scene.
/// Runs once after all persistent managers have initialized in Awake, then loads the Menu scene.
/// </summary>
public class Bootstrapper : MonoBehaviour {
    private void Start() {
        SceneLoader.Instance.LoadMenu();
    }
}
