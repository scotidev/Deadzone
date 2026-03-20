using UnityEngine;

/// <summary>
/// Attatched to "_Ghost" prefabs. Responsible for changing the ghost's material to green (valid) or red (invalid) based on whether the placement is valid. Controlled by BuildingController.
/// </summary>
public class GhostObject : MonoBehaviour {

    [Header("Materials")]

    [SerializeField] private Material validMaterial;
    [SerializeField] private Material invalidMaterial;

    private Renderer[] renderers;

    private bool isPlaceable = false;

    private void Awake() {
        renderers = GetComponentsInChildren<Renderer>();
    }

    /// <summary>
    /// Updates the visual appearance of the ghost object to indicate whether the associated place is available or occupied.
    /// </summary>
    /// <remarks>This method is typically called by the BuildingController each frame to reflect the current state of the place. It changes the material of all associated renderers to visually indicate availability.</remarks>
    /// <param name="placeable">A value indicating whether the place is available. 
    /// Specify <see langword="true"/> to show the place as available (green); 
    /// Specify <see langword="false"/> to show it as occupied (red).</param>
    public void SetPlaceable(bool placeable) {
        isPlaceable = placeable;

        Material mat = placeable ? validMaterial : invalidMaterial;

        foreach (Renderer r in renderers)
            r.material = mat;
    }

    public bool IsPlaceable() => isPlaceable;
}
